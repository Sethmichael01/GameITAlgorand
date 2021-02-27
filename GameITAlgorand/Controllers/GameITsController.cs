using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameITAlgorand.Data;
using GameITAlgorand.Models;
using Microsoft.AspNetCore.Identity;
using Algorand.Client;
using Algorand;
using Algorand.V2.Model;
using Algorand.V2;
using Account = Algorand.Account;
using Microsoft.AspNetCore.Authorization;

namespace GameITAlgorand.Controllers
{
    public class GameITsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private static UserManager<ApplicationUser> _userManager;

        public GameITsController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: GameITs
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            ViewBag.Address = user.AccountAddress;
            return View(await _context.GameIT.Include(x => x.User).ToListAsync());
        }

        // GET: GameITs/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var gameIT = await _context.GameIT.Include(x => x.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var key = user.Key;
            var receiver = gameIT.User.AccountAddress;
            var amount = gameIT.GamePrice;
            var senderAddr = user.AccountAddress;
            FundMethod(key, receiver, amount, senderAddr);
            ViewBag.Success = "Transaction was Sucessful";
            if (gameIT == null)
            {
                return NotFound();
            }

            return View(gameIT);
        }

        // GET: GameITs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: GameITs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,GameName,GamePrice,GameDescription")] GameIT gameIT)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                gameIT.User = user;
                _context.Add(gameIT);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(gameIT);
        }

        // GET: GameITs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameIT = await _context.GameIT.FindAsync(id);
            if (gameIT == null)
            {
                return NotFound();
            }
            return View(gameIT);
        }

        // POST: GameITs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,GameName,GamePrice,GameDescription")] GameIT gameIT)
        {
            if (id != gameIT.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gameIT);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameITExists(gameIT.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(gameIT);
        }

        // GET: GameITs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameIT = await _context.GameIT
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gameIT == null)
            {
                return NotFound();
            }

            return View(gameIT);
        }

        // POST: GameITs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gameIT = await _context.GameIT.FindAsync(id);
            _context.GameIT.Remove(gameIT);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Method to check if a game already exists by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool GameITExists(int id)
        {
            return _context.GameIT.Any(e => e.Id == id);
        }

        /// <summary>
        /// Method to fund or process transfers on the Algorand Network
        /// </summary>
        /// <param name="key"></param>
        /// <param name="receiver"></param>
        /// <param name="amount"></param>
        /// <param name="senderAddr"></param>
        public static void FundMethod(string key, string receiver, int amount, string senderAddr)
        {
            string ALGOD_API_ADDR = "https://testnet-algorand.api.purestake.io/ps2"; //find in algod.net
            string ALGOD_API_TOKEN = "B3SU4KcVKi94Jap2VXkK83xx38bsv95K5UZm2lab"; //find in algod.token          
            string SRC_ACCOUNT = key;
            string DEST_ADDR = receiver;
            Account src = new Account(SRC_ACCOUNT);
            AlgodApi algodApiInstance = new AlgodApi(ALGOD_API_ADDR, ALGOD_API_TOKEN);
            try
            {
                var trans = algodApiInstance.TransactionParams();
            }
            catch (ApiException e)
            {
                Console.WriteLine("Exception when calling algod#getSupply:" + e.Message);
            }

            TransactionParametersResponse transParams;
            try
            {
                transParams = algodApiInstance.TransactionParams();
            }
            catch (ApiException e)
            {
                throw new Exception("Could not get params", e);
            }
            var amountsent = Utils.AlgosToMicroalgos(amount);
            var tx = Utils.GetPaymentTransaction(src.Address, new Address(DEST_ADDR), amountsent, "pay message", transParams);
            var signedTx = src.SignTransaction(tx);

            Console.WriteLine("Signed transaction with txid: " + signedTx.transactionID);

            // send the transaction to the network
            try
            {
                var id = Utils.SubmitTransaction(algodApiInstance, signedTx);
                Console.WriteLine("Successfully sent tx with id: " + id.TxId);
                Console.WriteLine(Utils.WaitTransactionToComplete(algodApiInstance, id.TxId));
            }
            catch (ApiException e)
            {
                // This is generally expected, but should give us an informative error message.
                Console.WriteLine("Exception when calling algod#rawTransaction: " + e.Message);
            }
        }
    }
}
