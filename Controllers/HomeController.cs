using Microsoft.AspNetCore.Mvc;

namespace ChatInsightGemini.Controllers
{
    public class HomeController : Controller
    {
        private readonly GeminiClient _geminiClient;

        public HomeController(GeminiClient geminiClient)
        {
            _geminiClient = geminiClient;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string conversation, string type)
        {
            var result = await _geminiClient.AnalyzeChatAsync(conversation, type);
            ViewBag.AnalysisResult = result;
            return View();
        }
    }
}