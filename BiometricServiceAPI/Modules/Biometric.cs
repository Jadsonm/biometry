using BiometricService;
using Microsoft.AspNetCore.Mvc;
using NITGEN.SDK.NBioBSP;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using static NITGEN.SDK.NBioBSP.NBioAPI.Type;

public class Biometric
{
    private readonly APIService APIServiceInstance;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public Biometric(APIService apiService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        APIServiceInstance = apiService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }
    public IActionResult CaptureHash(bool img = false)
    {
        HFIR auditHFIR = new HFIR();
        APIServiceInstance._NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        uint ret = APIServiceInstance._NBioAPI.Capture(NBioAPI.Type.FIR_PURPOSE.ENROLL, out NBioAPI.Type.HFIR hCapturedFIR, NBioAPI.Type.TIMEOUT.DEFAULT, auditHFIR, null);

        APIServiceInstance._NBioAPI.GetFIRFromHandle(auditHFIR, out NBioAPI.Type.FIR auditFIR);
        int quality = auditFIR.Header.Quality;
        
        APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        if (ret != NBioAPI.Error.NONE) return new BadRequestObjectResult(
            new JsonObject
            {
                ["message"] = $"Error on Capture: {ret}",
                ["success"] = false
            }
        );

        NBioAPI.Export NBioExport = new NBioAPI.Export(APIServiceInstance._NBioAPI);
        NBioExport.NBioBSPToImage(auditHFIR, out NBioAPI.Export.EXPORT_AUDIT_DATA exportAuditData);

        string tempPath = Environment.ExpandEnvironmentVariables(@"%TEMP%\fingers-registered");

        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }

        DirectoryInfo directoryInfo = new DirectoryInfo(tempPath);
        FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);
        foreach (FileInfo file in files)
        {
            if (file.Extension.ToLower() == ".jpg")
            {
                file.Delete();
            }
        }

        APIServiceInstance._NBioAPI.GetTextFIRFromHandle(hCapturedFIR, out NBioAPI.Type.FIR_TEXTENCODE textFIR, true);

        string[] images = new string[10];
        List<byte> fingers = new List<byte> { };

        foreach (NBioAPI.Export.AUDIT_DATA finger in exportAuditData.AuditData)
        {
            APIServiceInstance._NBioAPI.ImgConvRawToJpgBuf(finger.Image[0].Data, exportAuditData.ImageWidth, exportAuditData.ImageHeight, 1, out byte[] imgData);
            Directory.CreateDirectory(tempPath);
            File.WriteAllBytes($"{tempPath}\\finger_{finger.FingerID}.jpg", imgData);
            images[finger.FingerID - 1] = Convert.ToBase64String(imgData);
            fingers.Add(finger.FingerID);
        }

        if (!img)
        {
            return new OkObjectResult(
                new JsonObject
                {
                    ["fingers-registered"] = exportAuditData.AuditData.GetLength(0),
                    ["template"] = textFIR.TextFIR,
                    ["fingers-id"] = new JsonArray(fingers.Select(finger => JsonValue.Create(finger)).ToArray()),
                    ["quality-FIR"] = quality,
                    ["success"] = true,
                }
            );
        }
        else
        {
            return new OkObjectResult(
                new JsonObject
                {
                    ["fingers-registered"] = exportAuditData.AuditData.GetLength(0),
                    ["template"] = textFIR.TextFIR,
                    ["fingers-id"] = new JsonArray(fingers.Select(finger => JsonValue.Create(finger)).ToArray()),
                    ["images"] = new JsonArray(images.Select(image => JsonValue.Create(image)).ToArray()),
                    ["quality-FIR"] = quality,
                    ["success"] = true,
                }
            );
        }
    }

    public IActionResult CaptureForVerify(uint windowVisibility = NBioAPI.Type.WINDOW_STYLE.POPUP)
    {
        HFIR auditHFIR = new HFIR();

        NBioAPI.Type.WINDOW_OPTION windowOption = new NBioAPI.Type.WINDOW_OPTION();
        windowOption.WindowStyle = windowVisibility;

        APIServiceInstance._NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        uint ret = APIServiceInstance._NBioAPI.Capture(NBioAPI.Type.FIR_PURPOSE.VERIFY, out NBioAPI.Type.HFIR hCapturedFIR, NBioAPI.Type.TIMEOUT.DEFAULT, auditHFIR, windowOption);
        APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        if (ret != NBioAPI.Error.NONE) return new BadRequestObjectResult(
            new JsonObject
            {
                ["message"] = $"Error on Capture: {ret}",
                ["success"] = false
            }
        );

        APIServiceInstance._NBioAPI.GetTextFIRFromHandle(hCapturedFIR, out NBioAPI.Type.FIR_TEXTENCODE textFIR, true);
        NBioAPI.Export NBioExport = new NBioAPI.Export(APIServiceInstance._NBioAPI);
        NBioExport.NBioBSPToImage(auditHFIR, out NBioAPI.Export.EXPORT_AUDIT_DATA exportAuditData);
        APIServiceInstance._NBioAPI.ImgConvRawToJpgBuf(exportAuditData.AuditData[0].Image[0].Data, exportAuditData.ImageWidth, exportAuditData.ImageHeight, 1, out byte[] imgData);
        string image64 = Convert.ToBase64String(imgData);

        return new OkObjectResult(
            new JsonObject
            {
                ["template"] = textFIR.TextFIR,
                ["image"] = image64,
                ["success"] = true
            }
        );
    }

    public IActionResult IdentifyOneOnOne(JsonObject template, bool img = false)
    {
        var rawTemplate = template["template"]?.ToString() ?? "";
        HFIR auditHFIR = new HFIR();
        uint ret;
        bool matched;

        APIServiceInstance._NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);

        if (rawTemplate.Contains('*'))
        {
            var storedFir = new NBioAPI.Type.FIR_TEXTENCODE { TextFIR = rawTemplate.TrimEnd('=') };
            ret = APIServiceInstance._NBioAPI.Verify(storedFir, out matched, null, -1, auditHFIR, null);
        }
        else
        {
            byte[] fim01HVBytes;
            try
            {
                string b64 = rawTemplate.Trim();
                int pad = b64.Length % 4;
                if (pad == 2) b64 += "==";
                else if (pad == 3) b64 += "=";
                fim01HVBytes = Convert.FromBase64String(b64);
            }
            catch (Exception ex)
            {
                APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
                var invalidChars = rawTemplate.Where(c => !((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/' || c == '=' || c == '-' || c == '_')).Distinct().ToArray();
                return new BadRequestObjectResult(new JsonObject
                {
                    ["message"] = $"Invalid template encoding: {ex.Message}",
                    ["template-length"] = rawTemplate.Length,
                    ["invalid-chars"] = new JsonArray(invalidChars.Select(c => JsonValue.Create((int)c)).ToArray()),
                    ["first-50-chars"] = rawTemplate.Length > 50 ? rawTemplate[..50] : rawTemplate,
                    ["success"] = false
                });
            }

            NBioAPI.Export exporter = new NBioAPI.Export(APIServiceInstance._NBioAPI);
            uint convRet = exporter.FDxToNBioBSP(fim01HVBytes, NBioAPI.Type.MINCONV_DATA_TYPE.MINCONV_TYPE_FIM01_HV, NBioAPI.Type.FIR_PURPOSE.VERIFY, out NBioAPI.Type.HFIR hStoredFIR);
            if (convRet != NBioAPI.Error.NONE)
            {
                APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
                return new BadRequestObjectResult(new JsonObject { ["message"] = $"Error converting FIM01_HV template: {convRet}", ["success"] = false });
            }

            ret = APIServiceInstance._NBioAPI.Verify(hStoredFIR, out matched, null, -1, auditHFIR, null);
        }

        APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        if (ret != NBioAPI.Error.NONE) return new BadRequestObjectResult(
            new JsonObject
            {
                ["message"] = ret == NBioAPI.Error.CAPTURE_TIMEOUT ? "Timeout" : $"Error on Verify: {ret}",
                ["success"] = false
            }
        );

        if (!img)
        {
            return new OkObjectResult(
                new JsonObject
                {
                    ["message"] = matched ? "Fingerprint matches" : "Fingerprint doesnt match",
                    ["success"] = matched
                }
            );
        }
        else
        {
            NBioAPI.Export NBioExport = new NBioAPI.Export(APIServiceInstance._NBioAPI);
            NBioExport.NBioBSPToImage(auditHFIR, out NBioAPI.Export.EXPORT_AUDIT_DATA exportAuditData);
            APIServiceInstance._NBioAPI.ImgConvRawToJpgBuf(exportAuditData.AuditData[0].Image[0].Data, exportAuditData.ImageWidth, exportAuditData.ImageHeight, 1, out byte[] imgData);
            string image64 = Convert.ToBase64String(imgData);

            return new OkObjectResult(
                new JsonObject
                {
                    ["message"] = matched ? "Fingerprint matches" : "Fingerprint doesnt match",
                    ["image"] = image64,
                    ["success"] = matched
                }
            );
        }
    }

    public IActionResult Identification(uint secuLevel = NBioAPI.Type.FIR_SECURITY_LEVEL.NORMAL)
    {
        APIServiceInstance._NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        uint ret = APIServiceInstance._NBioAPI.Capture(NBioAPI.Type.FIR_PURPOSE.VERIFY, out NBioAPI.Type.HFIR hCapturedFIR, NBioAPI.Type.TIMEOUT.DEFAULT, null, null);
        APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        if (ret != NBioAPI.Error.NONE) return new BadRequestObjectResult(
            new JsonObject
            {
                ["message"] = $"Error on Capture: {ret}",
                ["success"] = false
            }
        );

        NBioAPI.IndexSearch.CALLBACK_INFO_0 cbInfo = new();
        APIServiceInstance._IndexSearch.IdentifyData(hCapturedFIR, secuLevel, out NBioAPI.IndexSearch.FP_INFO fpInfo, cbInfo);

        return new OkObjectResult(
            new JsonObject
            {
                ["message"] = fpInfo.ID != 0 ? "Fingerprint match found" : "Fingerprint match not found",
                ["id"] = fpInfo.ID,
                ["success"] = fpInfo.ID != 0
            }
        );

    }

    public IActionResult LoadToMemory(JsonArray fingers)
    {
        if (fingers.Count == 0)
        {
            return new BadRequestObjectResult(
                new JsonObject
                {
                    ["message"] = "No templates to load",
                    ["success"] = false
                }
            );
        }

        uint ret;
        var exporter = new NBioAPI.Export(APIServiceInstance._NBioAPI);
        foreach (JsonObject fingerObject in fingers)
        {
            uint id = (uint)fingerObject["id"];
            string rawTemplate = fingerObject["template"]?.ToString() ?? "";

            if (rawTemplate.Contains('*'))
            {
                var textFir = new NBioAPI.Type.FIR_TEXTENCODE { TextFIR = rawTemplate.TrimEnd('=') };
                ret = APIServiceInstance._IndexSearch.AddFIR(textFir, id, out _);
            }
            else
            {
                byte[] fim01HVBytes;
                try
                {
                    string b64 = rawTemplate.Trim();
                    int pad = b64.Length % 4;
                    if (pad == 2) b64 += "==";
                    else if (pad == 3) b64 += "=";
                    fim01HVBytes = Convert.FromBase64String(b64);
                }
                catch (Exception ex)
                {
                    return new BadRequestObjectResult(new JsonObject
                    {
                        ["message"] = $"Invalid template encoding for id {id}: {ex.Message}",
                        ["success"] = false
                    });
                }

                uint convRet = exporter.FDxToNBioBSP(
                    fim01HVBytes,
                    NBioAPI.Type.MINCONV_DATA_TYPE.MINCONV_TYPE_FIM01_HV,
                    NBioAPI.Type.FIR_PURPOSE.ENROLL,
                    out NBioAPI.Type.HFIR hFIR);

                if (convRet != NBioAPI.Error.NONE)
                    return new BadRequestObjectResult(new JsonObject
                    {
                        ["message"] = $"Error converting FIM01_HV for id {id}: {convRet}",
                        ["success"] = false
                    });

                APIServiceInstance._NBioAPI.GetTextFIRFromHandle(hFIR, out NBioAPI.Type.FIR_TEXTENCODE convertedFir, false);
                ret = APIServiceInstance._IndexSearch.AddFIR(convertedFir, id, out _);
            }

            if (ret != NBioAPI.Error.NONE) return new BadRequestObjectResult(
                new JsonObject
                {
                    ["message"] = $"Error on AddFIR for id {id}: {ret}",
                    ["success"] = false
                }
            );
        }

        return new OkObjectResult(
            new JsonObject
            {
                ["message"] = "Templates loaded to memory",
                ["success"] = true
            }
        );
    }

    public IActionResult DeleteAllFromMemory()
    {
        APIServiceInstance._IndexSearch.ClearDB();
        return new OkObjectResult(
            new JsonObject
            {
                ["message"] = "All templates deleted from memory",
                ["success"] = true
            }
        );
    }

    public IActionResult TotalIdsInMemory()
    {
        APIServiceInstance._IndexSearch.GetDataCount(out UInt32 dataCount);
        return new OkObjectResult(
            new JsonObject
            {
                ["total"] = dataCount,
                ["success"] = true
            }
        );
    }

    public IActionResult DeviceUniqueSerialID()
    {
        APIServiceInstance._NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        byte[] input = new byte[8];
        APIServiceInstance._NBioAPI.DeviceIoControl(514, input, out byte[] deviceId);
        APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        return new OkObjectResult(
            new JsonObject
            {
                ["serial"] = BitConverter.ToString(deviceId),
                ["success"] = true
            }
        );
    }

    public IActionResult DebugFir()
    {
        APIServiceInstance._NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        uint ret = APIServiceInstance._NBioAPI.Capture(NBioAPI.Type.FIR_PURPOSE.ENROLL, out NBioAPI.Type.HFIR hFIR, NBioAPI.Type.TIMEOUT.DEFAULT, null, null);
        APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        if (ret != NBioAPI.Error.NONE) return new BadRequestObjectResult(new JsonObject { ["error"] = ret.ToString() });

        APIServiceInstance._NBioAPI.GetFIRFromHandle(hFIR, out NBioAPI.Type.FIR fir);
        APIServiceInstance._NBioAPI.GetTextFIRFromHandle(hFIR, out NBioAPI.Type.FIR_TEXTENCODE textFir, true);

        // Serialize FIR struct to bytes manually
        byte[] firBytes = new byte[4 + 4 + 4 + 2 + 2 + 2 + 2 + 4 + fir.Data.Length];
        int off = 0;
        BitConverter.GetBytes(fir.Format).CopyTo(firBytes, off); off += 4;
        BitConverter.GetBytes(fir.Header.Length).CopyTo(firBytes, off); off += 4;
        BitConverter.GetBytes(fir.Header.DataLength).CopyTo(firBytes, off); off += 4;
        BitConverter.GetBytes(fir.Header.Version).CopyTo(firBytes, off); off += 2;
        BitConverter.GetBytes(fir.Header.DataType).CopyTo(firBytes, off); off += 2;
        BitConverter.GetBytes(fir.Header.Purpose).CopyTo(firBytes, off); off += 2;
        BitConverter.GetBytes(fir.Header.Quality).CopyTo(firBytes, off); off += 2;
        BitConverter.GetBytes(fir.Header.Reserved).CopyTo(firBytes, off); off += 4;
        fir.Data.CopyTo(firBytes, off);

        return new OkObjectResult(new JsonObject
        {
            ["format"] = fir.Format,
            ["header-length"] = fir.Header.Length,
            ["header-datalength"] = fir.Header.DataLength,
            ["header-version"] = fir.Header.Version,
            ["header-datatype"] = fir.Header.DataType,
            ["header-purpose"] = fir.Header.Purpose,
            ["header-quality"] = fir.Header.Quality,
            ["header-reserved"] = fir.Header.Reserved,
            ["data-length"] = fir.Data.Length,
            ["fir-as-base64"] = Convert.ToBase64String(firBytes),
            ["text-fir"] = textFir.TextFIR,
            ["success"] = true
        });
    }

    public IActionResult StartContinuous(uint secuLevel)
    {
        var result = APIServiceInstance.StartContinuous(secuLevel);
        return new OkObjectResult(new JsonObject
        {
            ["message"] = result == "started" ? "Continuous identification started" : "Already running",
            ["running"] = APIServiceInstance.IsContinuousRunning,
            ["success"] = true
        });
    }

    public IActionResult StopContinuous()
    {
        var result = APIServiceInstance.StopContinuous();
        return new OkObjectResult(new JsonObject
        {
            ["message"] = result == "stopped" ? "Continuous identification stopped" : "Was not running",
            ["running"] = APIServiceInstance.IsContinuousRunning,
            ["success"] = true
        });
    }

    public IActionResult ContinuousStatus()
    {
        var last = APIServiceInstance.LastResult;
        return new OkObjectResult(new JsonObject
        {
            ["running"] = APIServiceInstance.IsContinuousRunning,
            ["last-id"] = last?.Id ?? 0,
            ["last-success"] = last?.Success ?? false,
            ["last-timestamp"] = last?.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            ["success"] = true
        });
    }

    public IActionResult GetIdentificationLog()
    {
        var log = APIServiceInstance.IdentificationLog;
        var arr = new JsonArray(log.Select(r => (JsonNode)new JsonObject
        {
            ["id"] = r.Id,
            ["timestamp"] = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
        }).ToArray());
        return new OkObjectResult(new JsonObject
        {
            ["results"] = arr,
            ["count"] = log.Count,
            ["success"] = true
        });
    }

    public IActionResult LoadFromDb()
    {
        var (loaded, errors, exception) = APIServiceInstance.LoadFromDatabase();
        bool success = exception == null;
        return new OkObjectResult(new JsonObject
        {
            ["message"] = exception ?? $"Loaded {loaded} templates from Oracle ({errors} errors)",
            ["loaded"] = loaded,
            ["errors"] = errors,
            ["exception"] = exception,
            ["success"] = success
        });
    }

    public async Task<IActionResult> LoadFromSeniorAsync()
    {
        var cfg = _configuration.GetSection("Senior");
        string apiUrl   = cfg["ApiUrl"]     ?? "https://api.senior.com.br";
        string clientId = cfg["ClientId"]   ?? "";
        string accessKey = cfg["AccessKey"] ?? "";
        string secret    = cfg["Secret"]    ?? "";
        string tenantName = cfg["TenantName"] ?? "";

        var http = _httpClientFactory.CreateClient();

        // 1. Login
        string token;
        try
        {
            var loginBody = JsonSerializer.Serialize(new { accessKey, secret, tenantName });
            var req = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/platform/authentication/anonymous/loginWithKey");
            req.Headers.Add("client_id", clientId);
            req.Content = new StringContent(loginBody);
            req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var resp = await http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var jsonTokenStr = body.GetProperty("jsonToken").GetString()!;
            var jsonToken = JsonSerializer.Deserialize<JsonElement>(jsonTokenStr);
            token = jsonToken.GetProperty("access_token").GetString()!;
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new JsonObject { ["message"] = $"Senior login failed: {ex.Message}", ["success"] = false });
        }

        var pairs = new List<(uint id, string template)>();
        int totalBiometries = 0;
        try
        {
                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"{apiUrl}/sam/application/entities/biometry?filter=person.situation='ACTIVE'&biometricManufacturer=FINGERPRINT_NITGEN&size=1000");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                req.Headers.Add("client_id", clientId);
                var resp = await http.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("totalElements", out var te)) totalBiometries = te.GetInt32();
                foreach (var bio in json.GetProperty("contents").EnumerateArray())
                {
                    var registryStr = bio.GetProperty("person").GetProperty("registry").GetString() ?? "";
                    if (!int.TryParse(registryStr, out int registry)) continue;
                    uint uid = (uint)registry;
                    foreach (var tpl in bio.GetProperty("templates").EnumerateArray())
                    {
                        var tplStr = tpl.GetProperty("template").GetString() ?? "";
                        if (!string.IsNullOrEmpty(tplStr))
                            pairs.Add((uid, tplStr));
                    }
                }
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new JsonObject { ["message"] = $"Error fetching biometrics: {ex.Message}", ["success"] = false });
        }

        // Agrupa templates por pessoa e carrega na memória
        APIServiceInstance._IndexSearch.ClearDB();
        var exporter = new NBioAPI.Export(APIServiceInstance._NBioAPI);
        int loaded = 0, errors = 0;

        // Agrupa: id → lista de templates
        var byPerson = new Dictionary<uint, List<string>>();
        foreach (var (id, tpl) in pairs)
        {
            if (!byPerson.TryGetValue(id, out var list))
                byPerson[id] = list = new List<string>();
            list.Add(tpl);
        }

        foreach (var (id, templates) in byPerson)
        {
            // Converte cada template para TextFIR
            var textFirs = new List<NBioAPI.Type.FIR_TEXTENCODE>();
            bool convFailed = false;
            foreach (var templateStr in templates)
            {
                NBioAPI.Type.FIR_TEXTENCODE textFir;
                if (templateStr.Contains('*'))
                {
                    textFir = new NBioAPI.Type.FIR_TEXTENCODE { TextFIR = templateStr.TrimEnd('=') };
                }
                else
                {
                    byte[] bytes;
                    try
                    {
                        string b64 = templateStr.Trim();
                        int pad = b64.Length % 4;
                        if (pad == 2) b64 += "==";
                        else if (pad == 3) b64 += "=";
                        bytes = Convert.FromBase64String(b64);
                    }
                    catch { convFailed = true; break; }

                    uint convRet = exporter.FDxToNBioBSP(bytes, NBioAPI.Type.MINCONV_DATA_TYPE.MINCONV_TYPE_FIM01_HV, NBioAPI.Type.FIR_PURPOSE.ENROLL, out NBioAPI.Type.HFIR hFIR);
                    if (convRet != NBioAPI.Error.NONE) { convFailed = true; break; }

                    APIServiceInstance._NBioAPI.GetTextFIRFromHandle(hFIR, out textFir, false);
                }
                textFirs.Add(textFir);
            }

            if (convFailed) { errors++; continue; }

            // Mescla múltiplos dedos via CreateTemplate antes de AddFIR
            NBioAPI.Type.FIR_TEXTENCODE finalTextFir = textFirs[0];
            for (int i = 1; i < textFirs.Count; i++)
            {
                APIServiceInstance._NBioAPI.CreateTemplate(finalTextFir, textFirs[i], out NBioAPI.Type.HFIR hMerged, null);
                APIServiceInstance._NBioAPI.GetTextFIRFromHandle(hMerged, out finalTextFir, false);
            }

            uint addRet = APIServiceInstance._IndexSearch.AddFIR(finalTextFir, id, out _);
            if (addRet != NBioAPI.Error.NONE) errors++;
            else loaded++;
        }

        return new OkObjectResult(new JsonObject
        {
            ["message"] = $"Loaded {loaded} templates from Senior ({errors} errors)",
            ["total-biometries-in-senior"] = totalBiometries,
            ["templates-fetched"] = pairs.Count,
            ["people-processed"] = byPerson.Count,
            ["loaded"] = loaded,
            ["errors"] = errors,
            ["success"] = errors == 0
        });
    }

    public IActionResult JoinTemplates(JsonArray fingers)
    {
        if (fingers.Count < 2) return new BadRequestObjectResult(
                                   new JsonObject
                                   {
                                       ["message"] = "No templates to join",
                                       ["success"] = false
                                   });

        List<string> list = [];
        list.AddRange(fingers.Select(fingerObject => fingerObject["template"].ToString()));

        NBioAPI.Type.FIR_PAYLOAD payload = new NBioAPI.Type.FIR_PAYLOAD();
        for (int i = 1; i < fingers.Count; i++)
        {
            NBioAPI.Type.FIR_TEXTENCODE textFIR1 = new NBioAPI.Type.FIR_TEXTENCODE() { TextFIR = list[i - 1] };
            NBioAPI.Type.FIR_TEXTENCODE textFIR2 = new NBioAPI.Type.FIR_TEXTENCODE() { TextFIR = list[i] };
            APIServiceInstance._NBioAPI.CreateTemplate(textFIR1, textFIR2, out NBioAPI.Type.HFIR hNew, payload);
            uint ret = APIServiceInstance._NBioAPI.GetTextFIRFromHandle(hNew, out NBioAPI.Type.FIR_TEXTENCODE newTextFIR, false);
            if (ret != NBioAPI.Error.NONE) return new BadRequestObjectResult(
                                   new JsonObject
                                   {
                                       ["message"] = $"Error creating template: {ret}",
                                       ["success"] = false
                                   });
            list[i] = newTextFIR.TextFIR;
        }
        return new OkObjectResult(
            new JsonObject
            {
                ["template"] = list[fingers.Count - 1],
                ["message"] = $"Templates joined successfully",
                ["success"] = true
            });
    }
}