using CoilBin.RackApi.Hubs.Rack;
using CoilBin.RackApi.Models;
using CoilBin.RackApi.Requests;
using CoilBin.RackApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Crypto.Signers;

namespace CoilBin.RackApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RackController : ControllerBase
    {
        private readonly RackSaleableServices rackDataServices;
        private readonly RackPLCService rackPLCService;
        private readonly IHubContext<RackHub, IRackClient> rackHub;
        public RackController(RackSaleableServices rackDataServices,RackPLCService rackPLCService,IHubContext<RackHub,IRackClient> rackHub)
        {
            this.rackDataServices = rackDataServices;
            this.rackPLCService = rackPLCService;
            this.rackHub = rackHub;
        }

        [HttpGet("")]
        public async Task<IEnumerable<RackModel>> GetRackList()
        {
            return await rackDataServices.GetRackList();
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            return await rackDataServices.Login(model.password) ? Ok() : Unauthorized();
        }

        [HttpPost("Transaksi")]
        public async Task<IActionResult> Transaksi(Step2TransaksiRequest req)
        {
            var rack = await rackDataServices.GetRack(req.name);
            var container = await rackDataServices.GetContainer(req.containerName);
            var waste = await rackDataServices.GetWaste(req.waste);

            var payload = req.payload;
            bool isCollection = payload.type == "Collection";
            payload.idWaste = waste!.wasteId;
            payload.idContainer = container!.containerId;
            payload.weight = isCollection ? 0 : (payload.weight + (rack?.weight ?? 0));
            await rackDataServices.UpdateRackWeight(rack.rackId, payload.weight);
            rack = await rackDataServices.GetRack(rack.rackId);
            await rackHub.Clients.All.weightUpdated(rack);
            return Ok(new { msg = await rackDataServices.SaveTransaksi(payload) });
            
        }

        [HttpPost("CheckCapacity")]
        public async Task<IEnumerable<RackModel>> CheckCapacity(Step2CapacityRackAvailableRequest req)
        {
            var racks = await rackDataServices.GetRacks(req.line.ToString());
            racks = racks.Where(x => (x.weight + req.weight) < x.max_weight).OrderBy(x => x.rackId);
            if (!racks.Any())
                throw new HttpRequestException("No Rack Available", null, System.Net.HttpStatusCode.NotFound);
            foreach (var rack in racks)
            {
                bool res = await rackPLCService.TriggerRack(rack, true);
                if (res || true)
                    return [rack];
            }
            throw new HttpRequestException("All of Rack Doors can't be opened", null, System.Net.HttpStatusCode.InternalServerError);
        }


        [HttpGet("Door/{rackId}")]
        public async Task<IActionResult> TriggerRackDoor(int rackId,bool enable=true)
        {
            var rack = await rackDataServices.GetRack(rackId);
            if (rack is null)
                throw new HttpRequestException("No Rack Found", null, System.Net.HttpStatusCode.NotFound);
            return await rackPLCService.TriggerRack(rack,enable) ? Ok(new { msg = "ok" }) : StatusCode(500,"Error Can't open rack door");
        }

    }
}
