using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Librum.Interfaces;
using Librum.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Slugify;

namespace Librum.Controllers
{
    // [Authorize]
    [Route("article")]
    public class ArticleController : Controller
    {
        private IArticles _articles;

        public ArticleController(IArticles articles)
        {
            _articles = articles;
        }
        
        [Authorize]
        [Route("new-post")]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [Route("new-post")]
        public async Task<IActionResult> New(Article article)
        {
            if(ModelState.IsValid)
            {
                article.Slug = Slug(article.Title);
                article.WritedDatetime = DateTime.Now;
                article.IsDraft = false;
                article.Description = Truncate(article.Content, 300, "…", true);
                article.AuthorUsername = User.Identity.Name;
                await _articles.NewArticleAsync(article);
                return RedirectToAction("Article", new { slugArticle = article.Slug });
            }
            return View(article);
        }

        [AllowAnonymous]
        [Route("{slugArticle}")]
        public async Task<IActionResult> Article(string slugArticle)
        {
            var article = await _articles.GetArticleBySlugAsync(slugArticle);
            return View(article);
        }

        private static string Truncate(string value, int length, string ellipsis, bool keepFullWordAtEnd)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length < length) return value;
            value = value.Substring(0, length);
            if (keepFullWordAtEnd)
            {
                value = value.Substring(0, value.LastIndexOf(' '));
            }
            return value + ellipsis;
        }

        private static string Slug(string value)
        {
            return new SlugHelper().GenerateSlug(RemoveSpecialCharacters(value).ToLower().Trim()).ToString();
        }

        private static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_. ]+", "", RegexOptions.Compiled);
        }
    }
}