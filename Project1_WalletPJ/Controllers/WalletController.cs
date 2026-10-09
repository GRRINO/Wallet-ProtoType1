using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project1_WalletPJ.Features;
using static Project1_WalletPJ.Dtos.WalletDtos;

namespace Project1_WalletPJ.Controllers
{
    [ApiController]
    [Route("api/wallets")]
    public class WalletController : ControllerBase
    {
        public readonly BL_Account _bl;
        public WalletController(BL_Account bl)
        {
            _bl = bl;
        }


        [HttpPost]
        public async Task<IActionResult> Register(RegisterRequest r) =>
            Created($"api/wallets/{r.MobileNo}", await _bl.RegisterAsync(r));

        [HttpGet("{mobileNo}")]
        public async Task<IActionResult> Get(string mobileNo) => Ok(await _bl.GetWalletAsync(mobileNo));

        [HttpPost("{mobileNo}/deposit")]
        public async Task<IActionResult> Deposit(string mobileNo, AmountRequest r) =>
            Ok(await _bl.DepositAsync(mobileNo, r.Amount, r.Note));

        [HttpPost("{mobileNo}/withdraw")]
        public async Task<IActionResult> Withdraw(string mobileNo, WithdrawRequest r) =>
            Ok(await _bl.WithdrawAsync(mobileNo, r.Amount, r.Pin, r.Note));

        [HttpPost("{mobileNo}/transfer")]
        public async Task<IActionResult> Transfer(string mobileNo, TransferRequest r) =>
            Ok(await _bl.TransferAsync(mobileNo, r));

        [HttpGet("{mobileNo}/statement")]
        public async Task<IActionResult> Statement(string mobileNo, DateTime? from, DateTime? to,
            int page = 1, int pageSize = 20) =>
            Ok(await _bl.GetStatementAsync(mobileNo, from, to, page, pageSize));

        [HttpPut("{mobileNo}/pin")]
        public async Task<IActionResult> ChangePin(string mobileNo, ChangePinRequest r)
        {
            await _bl.ChangePinAsync(mobileNo, r);
            return NoContent();
        }

        [HttpPut("{mobileNo}/activate")]
        public async Task<IActionResult> Activate(string mobileNo) { await _bl.SetActiveAsync(mobileNo, true); return NoContent(); }

        [HttpPut("{mobileNo}/deactivate")]
        public async Task<IActionResult> Deactivate(string mobileNo) { await _bl.SetActiveAsync(mobileNo, false); return NoContent(); }
    }
}
