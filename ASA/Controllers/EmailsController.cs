using Microsoft.AspNetCore.Mvc;
using IndustrialSolutions.Services;
using IndustrialSolutions.Models;
using Microsoft.AspNetCore.Authorization;


namespace IndustrialSolutions.Controllers;


public class EmailsController : Controller
{
    private readonly EmailCache _cache;
    private readonly ImapEmailReader _reader;


    public EmailsController(EmailCache cache, ImapEmailReader reader)
    {
        _cache = cache;
        _reader = reader; 
    }


    // Grid page
    [HttpGet]
    public IActionResult Index() => View();


    // DataTables source
    [HttpGet("api/emails")]
    public IActionResult List()
    {
        var items = _cache.List();
        return Json(new { data = items });
    }


    // Detail load (fetch full on-demand to keep sync fast)
    [HttpGet("api/emails/{id}")]
    public async Task<IActionResult> Detail(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();
        if (!uint.TryParse(id.Split('@')[0], out var uid)) return BadRequest();
        var (dto, _) = await _reader.LoadFullAsync(uid, HttpContext.RequestAborted);
        return Json(dto);
    }


    // Download attachment
    //[HttpGet("api/emails/{id}/attachment")]
    //public async Task<IActionResult> Download(string id, string fileName)
    //{
    //    if (!uint.TryParse(id.Split('@')[0], out var uid)) return BadRequest();
    //    var (stream, contentType, name) = await _reader.DownloadAttachmentAsync(uid, fileName, HttpContext.RequestAborted);
    //    return File(stream, contentType, name);
    //}
}