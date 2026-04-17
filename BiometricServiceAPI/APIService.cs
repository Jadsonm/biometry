using NITGEN.SDK.NBioBSP;
using Oracle.ManagedDataAccess.Client;
using static NITGEN.SDK.NBioBSP.NBioAPI.Type;

namespace BiometricService
{
    public record IdentificationResult(uint Id, bool Success, DateTime Timestamp);

    public sealed class APIService : BackgroundService
    {
        private readonly ILogger<APIService> _logger;
        private readonly IConfiguration _configuration;
        public NBioAPI _NBioAPI;
        public NBioAPI.IndexSearch _IndexSearch;

        private CancellationTokenSource? _continuousCts;
        private Task? _continuousTask;
        public IdentificationResult? LastResult { get; private set; }
        public bool IsContinuousRunning => _continuousCts != null && !_continuousCts.IsCancellationRequested;

        private readonly List<IdentificationResult> _identificationLog = new();
        private readonly object _logLock = new();
        public IReadOnlyList<IdentificationResult> IdentificationLog { get { lock (_logLock) return _identificationLog.ToList(); } }
        private string LogFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "identification_log.txt");

        public APIService(ILogger<APIService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _NBioAPI = new NBioAPI();
            _IndexSearch = new NBioAPI.IndexSearch(_NBioAPI);
            _IndexSearch.InitEngine();

            string filepath = "C:\\Windows\\System32\\NBSP2Por.dll";
            _NBioAPI.SetSkinResource(filepath);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Biometric API Service is starting.");
            try
            {
                _logger.LogInformation("Biometric API Service is running.");

                await Task.Run(() => LoadFromDatabase(), stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _NBioAPI.Dispose();
                _IndexSearch.TerminateEngine();
                _logger.LogInformation("Biometric API Service has been stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Biometric API Service: {Message}", ex.Message);
                Environment.Exit(1);
            }
        }

        public string StartContinuous(uint secuLevel)
        {
            if (IsContinuousRunning)
                return "already_running";

            lock (_logLock) _identificationLog.Clear();

            _continuousCts = new CancellationTokenSource();
            var token = _continuousCts.Token;

            _continuousTask = Task.Run(async () =>
            {
                _NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        uint ret = _NBioAPI.Capture(
                            NBioAPI.Type.FIR_PURPOSE.VERIFY,
                            out NBioAPI.Type.HFIR hFIR,
                            3000,   // 3 segundos de timeout por tentativa
                            null, null);

                        if (token.IsCancellationRequested) break;

                        if (ret == NBioAPI.Error.NONE)
                        {
                            var cbInfo = new NBioAPI.IndexSearch.CALLBACK_INFO_0();
                            _IndexSearch.IdentifyData(hFIR, secuLevel, out NBioAPI.IndexSearch.FP_INFO fpInfo, cbInfo);
                            var result = new IdentificationResult(fpInfo.ID, fpInfo.ID != 0, DateTime.Now);
                            LastResult = result;

                            if (fpInfo.ID != 0)
                            {
                                lock (_logLock) _identificationLog.Add(result);
                                File.AppendAllText(LogFilePath,
                                    $"{result.Timestamp:yyyy-MM-dd HH:mm:ss} | ID: {result.Id}\n");
                            }
                        }

                        await Task.Delay(200, token).ContinueWith(_ => { });
                    }
                }
                finally
                {
                    _NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
                }
            }, token);

            return "started";
        }

        public string StopContinuous()
        {
            if (!IsContinuousRunning)
                return "not_running";

            _continuousCts!.Cancel();
            _continuousCts = null;
            return "stopped";
        }

        public (int loaded, int errors, string? exception) LoadFromDatabase()
        {
            int loaded = 0;
            int errors = 0;
            string? connStr = _configuration.GetValue<string>("OracleConnection");

            if (string.IsNullOrWhiteSpace(connStr) || connStr.Contains("SENHA_AQUI"))
            {
                _logger.LogWarning("OracleConnection not configured. Skipping database load.");
                return (0, 0, "OracleConnection not configured or password placeholder not replaced.");
            }

            try
            {
                _IndexSearch.ClearDB();

                using var conn = new OracleConnection(connStr);
                conn.Open();

                using var cmd = new OracleCommand("SELECT ID, BIOMETRIA FROM biometria_templates", conn);
                using var reader = cmd.ExecuteReader();
                var exporter = new NBioAPI.Export(_NBioAPI);

                while (reader.Read())
                {
                    uint id;
                    try { id = Convert.ToUInt32(reader["ID"]); }
                    catch { errors++; continue; }

                    byte[] blob;
                    try { blob = (byte[])reader["BIOMETRIA"]; }
                    catch { errors++; continue; }

                    uint convRet = exporter.FDxToNBioBSP(
                        blob,
                        MINCONV_DATA_TYPE.MINCONV_TYPE_FIM01_HV,
                        FIR_PURPOSE.ENROLL,
                        out HFIR hFIR);

                    if (convRet != NBioAPI.Error.NONE)
                    {
                        _logger.LogDebug("FDxToNBioBSP failed for ID {Id}: {Err}", id, convRet);
                        errors++;
                        continue;
                    }

                    _NBioAPI.GetTextFIRFromHandle(hFIR, out NBioAPI.Type.FIR_TEXTENCODE convertedFir, false);
                    uint addRet = _IndexSearch.AddFIR(convertedFir, id, out _);
                    if (addRet != NBioAPI.Error.NONE)
                    {
                        _logger.LogDebug("AddFIR failed for ID {Id}: {Err}", id, addRet);
                        errors++;
                        continue;
                    }

                    loaded++;
                }

                _logger.LogInformation("Database load complete. Loaded: {Loaded}, Errors: {Errors}", loaded, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading templates from Oracle: {Message}", ex.Message);
                return (loaded, errors, ex.Message);
            }

            return (loaded, errors, null);
        }
    }
}
