using Microsoft.AspNetCore.Mvc;
using NITGEN.SDK.NBioBSP;
using System.Text.Json.Nodes;

namespace BiometricService.Controllers
{
    [ApiController]
    [Route("apiservice/")]
    public class APIController : ControllerBase
    {
        private readonly Biometric _biometric;

        public APIController(Biometric biometric)
        {
            _biometric = biometric;
        }

        [HttpGet("capture-hash")]
        public IActionResult Capture(bool? img)
        {
            if (img.HasValue)
            {
                return _biometric.CaptureHash((bool)img);
            }
            else
            {
                return _biometric.CaptureHash();
            }
        }

        [HttpGet("capture-for-verify")]
        public IActionResult CaptureForVerify(uint? window)
        {
            if (window.HasValue)
            {
                return _biometric.CaptureForVerify((uint)window);
            }
            else
            {
                return _biometric.CaptureForVerify();
            }
        }

        [HttpPost("match-one-on-one")]
        public IActionResult MatchOneOnOne([FromBody] JsonObject template, bool? img)
        {
            if (img.HasValue)
            {
                return _biometric.IdentifyOneOnOne(template, (bool)img);
            }
            else
            {
                return _biometric.IdentifyOneOnOne(template);
            }
        }

        [HttpGet("identification")]
        public IActionResult Identification(uint? secuLevel)
        {
            if (secuLevel.HasValue)
            {
                return _biometric.Identification((uint)secuLevel);
            }
            else
            {
                return _biometric.Identification();
            }
        }

        [HttpPost("load-to-memory")]
        public IActionResult LoadToMemory([FromBody] JsonArray fingers)
        {
            return _biometric.LoadToMemory(fingers);
        }

        [HttpGet("delete-all-from-memory")]
        public IActionResult DeleteAllFromMemory()
        {
            return _biometric.DeleteAllFromMemory();
        }

        [HttpGet("total-in-memory")]
        public IActionResult TotalIdsInMemory()
        {
            return _biometric.TotalIdsInMemory();
        }

        [HttpGet("device-unique-id")]
        public IActionResult DeviceUniqueSerialID()
        {
            return _biometric.DeviceUniqueSerialID();
        }

        [HttpPost("join-templates")]
        public IActionResult JoinTemplates([FromBody] JsonArray fingers)
        {
            return _biometric.JoinTemplates(fingers);
        }

        [HttpPost("load-from-db")]
        public IActionResult LoadFromDb()
        {
            return _biometric.LoadFromDb();
        }

        [HttpPost("identification/start")]
        public IActionResult StartContinuousIdentification(uint? secuLevel)
        {
            return _biometric.StartContinuous(secuLevel ?? NBioAPI.Type.FIR_SECURITY_LEVEL.NORMAL);
        }

        [HttpPost("identification/stop")]
        public IActionResult StopContinuousIdentification()
        {
            return _biometric.StopContinuous();
        }

        [HttpGet("identification/status")]
        public IActionResult ContinuousIdentificationStatus()
        {
            return _biometric.ContinuousStatus();
        }

        [HttpGet("identification/results")]
        public IActionResult IdentificationResults()
        {
            return _biometric.GetIdentificationLog();
        }

        [HttpPost("load-from-senior")]
        public async Task<IActionResult> LoadFromSenior()
        {
            return await _biometric.LoadFromSeniorAsync();
        }

        [HttpGet("debug-fir")]
        public IActionResult DebugFir()
        {
            return _biometric.DebugFir();
        }
    }
}