using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Markdig;
using Slugify;
using Librum.Interfaces;
using Librum.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Librum.Controllers
{
    [Authorize]
    [Route("article")]
    public class ArticleController : Controller
    {
        private readonly Articles _articles;

        public ArticleController(Articles articles)
        {
            _articles = articles;
        }
        
        [Route("new-post")]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [Route("new-post")]
        public async Task<IActionResult> New(Article article)
        {
            if(ModelState.IsValid)
            {
                article.Slug = Slug(article.Title);
                article.Title = article.Title;
                article.WritedDatetime = DateTime.Now;
                article.Content = article.Content;
                article.Description = Truncate(Markdown.ToPlainText(article.Content), 300, "…", true);
                article.AuthorUsername = User.Identity.Name;
                article.Keywords = article.Keywords;
                article.IsDraft = article.IsDraft;
                article.Unlisted = article.Unlisted;
                article.LikeCount = 0;
                article.ReadCount = 0;
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
            if(article == null)
            {
                return RedirectToAction("Error", "Home");
            }
            return View(article);
        }

        [Route("{slugArticle}/edit")]
        public async Task<IActionResult> Edit(string slugArticle)
        {
            var article = await _articles.GetArticleBySlugAsync(slugArticle);
            return View(article);
        }

        [HttpPost]
        [Route("{slugArticle}/edit")]
        public async Task<IActionResult> Edit(string slugArticle, Article article)
        {
            if (ModelState.IsValid)
            {
                article.Slug = Slug(article.Title);
                article.Title = article.Title;
                article.WritedDatetime = DateTime.Now;
                article.Content = article.Content;
                article.Description = Truncate(Markdown.ToPlainText(article.Content), 300, "…", true);
                article.AuthorUsername = User.Identity.Name;
                article.Keywords = article.Keywords;
                article.IsDraft = article.IsDraft;
                article.Unlisted = article.Unlisted;
                await _articles.EditArticleAsync(slugArticle, article);
                return RedirectToAction("Article", new { slugArticle = article.Slug });
            }
            return View(article);
        }

        [Route("{slugArticle}/delete")]
        public async Task<IActionResult> Delete(string slugArticle)
        {
            await _articles.DeleteArticleAsync(slugArticle);
            return RedirectToAction("Index", "Home");
        }

        [Route("{slugArticle}/publish")]
        public async Task<IActionResult> Publish(string slugArticle)
        {
            await _articles.PublishArticleAsync(slugArticle);
            return RedirectToAction("Index", "Home");
        }

        [Route("{slugArticle}/like")]
        public async Task<IActionResult> Like(string slugArticle)
        {
            var article = await _articles.GetArticleBySlugAsync(slugArticle);
            if (article == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var liked = new List<string>();
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Liked")))
            {
                liked = JsonConvert.DeserializeObject<string[]>(HttpContext.Session.GetString("Liked")).ToList();
            }
            liked.Add(article.Id);
            HttpContext.Session.SetString("Liked", JsonConvert.SerializeObject(liked));
            return Ok();
        }

        [Route("{slugArticle}/unlike")]
        public async Task<IActionResult> Unlike(string slugArticle)
        {
            var article = await _articles.GetArticleBySlugAsync(slugArticle);
            if (article == null)
            {
                return RedirectToAction("Error", "Home");
            }

            try
            {
                var liked = JsonConvert.DeserializeObject<string[]>(HttpContext.Session.GetString("Liked")).ToList();
                liked.Remove(article.Id);
                HttpContext.Session.SetString("Liked", JsonConvert.SerializeObject(liked));
            }
            catch { }
            return Ok();
        }

        [Route("{slugArticle}/bookmark")]
        public async Task<IActionResult> Bookmark(string slugArticle)
        {
            var article = await _articles.GetArticleBySlugAsync(slugArticle);
            if (article == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var bookmarks = new List<string>();
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Bookmarks")))
            {
                bookmarks = JsonConvert.DeserializeObject<string[]>(HttpContext.Session.GetString("Bookmarks")).ToList();
            }
            bookmarks.Add(article.Id);
            HttpContext.Session.SetString("Bookmarks", JsonConvert.SerializeObject(bookmarks));
            return Ok();
        }

        [Route("{slugArticle}/unbookmark")]
        public async Task<IActionResult> Unbookmark(string slugArticle)
        {
            var article = await _articles.GetArticleBySlugAsync(slugArticle);
            if (article == null)
            {
                return RedirectToAction("Error", "Home");
            }

            try
            {
                var liked = JsonConvert.DeserializeObject<string[]>(HttpContext.Session.GetString("Bookmarks")).ToList();
                liked.Remove(article.Id);
                HttpContext.Session.SetString("Bookmarks", JsonConvert.SerializeObject(liked));
            }
            catch { }
            return Ok();
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