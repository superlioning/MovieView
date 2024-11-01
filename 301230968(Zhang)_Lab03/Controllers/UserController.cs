using _301230968_Zhang__Lab03.Models;
using Microsoft.AspNetCore.Mvc;

namespace _301230968_Zhang__Lab03.Controllers
{
    public class UserController : Controller
    {
        private readonly Models.MovieContext _context;

        public UserController(MovieContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View(_context.Users.ToList());
        }

        //Registration
        public ActionResult Register()
        {
            return View();
        }
        //POST
        [HttpPost]
        public ActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                user.RegisterDate = DateTime.Now; // Set RegisterDate to the current date and time
                _context.Users.Add(user);
                _context.SaveChanges();
                ModelState.Clear();
                TempData["Message"] = "Hi " + user.FirstName + " " + user.LastName + ", your account has been successfully registered.";

                return RedirectToAction("Login");
            }
            return View();
        }

        //Login
        public ActionResult Login()
        {
            return View();
        }
        //POST
        [HttpPost]
        public ActionResult Login(User user)
        {
            var usr = _context.Users.SingleOrDefault(u => u.Username == user.Username && u.Password == user.Password);
            if (usr != null)
            {
                HttpContext.Session.SetString("UserId", usr.UserId.ToString());
                HttpContext.Session.SetString("Username", usr.Username.ToString());
                return RedirectToAction("Index", "Movie");
            }
            else
            {
                ModelState.AddModelError("", "Username or Password is wrong.");
            }
            return View();
        }
        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
