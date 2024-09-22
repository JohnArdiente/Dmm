using Dmm.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Font = iTextSharp.text.Font;
using static System.Runtime.InteropServices.JavaScript.JSType;
using iTextSharp.text.pdf.draw;
using Image = iTextSharp.text.Image;
using Microsoft.AspNetCore.Hosting;

namespace Dmm.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

		private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult GetItems()
        {
            var manualMatches = _context.ManualMatches
                .Select(x => new { x.EntryId1, x.EntryId2 })
                .ToList();

            var entries = _context.Entry
                .Include(e => e.EntryData)
                .ToList();

            var filteredEntries = entries
                .Select(e => new Entry
                {
                    EntryId = e.EntryId,
                    EntryName = e.EntryName,
                    Bet = e.Bet,
                    EntryData = e.EntryData
                        .Where(ed => !manualMatches.Any(mm => mm.EntryId1 == ed.Id || mm.EntryId2 == ed.Id))
                        .Select(ed => new EntryData
                        {
                            Id = ed.Id,
                            Weight = ed.Weight
                        })
                        .OrderBy(ed => ed.Weight)
                        .ToList()
                })
                .Where(e => e.EntryData.Any())
                .ToList();

            return Json(filteredEntries);
        }

        [HttpPost]
        public IActionResult SaveManualMatchedEntries([FromBody] List<MatchedEntryViewModel> matchedEntries)
        {

            try
            {

                var token = new Token
                {
                    Value = Guid.NewGuid(),
                    CreateAt = DateTime.Now
                };

                _context.Token.Add(token);
                _context.SaveChanges();

                foreach (var entry in matchedEntries)
                {
                    if (entry.Id1 == 0 || entry.Id2 == 0 || string.IsNullOrEmpty(entry.EntryName1) || string.IsNullOrEmpty(entry.EntryName2))
                    {
                        continue;
                    }

                    bool exists = _context.ManualMatches.Any(e => e.EntryId1 == entry.Id1 && e.EntryId2 == entry.Id2);

                    if (!exists)
                    {
                        var matchedEntry = new ManualMatches
                        {
                            EntryId1 = entry.Id1,
                            EntryName1 = entry.EntryName1,
                            Weight1 = decimal.Parse(entry.Weight1),
                            EntryId2 = entry.Id2,
                            EntryName2 = entry.EntryName2,
                            Weight2 = decimal.Parse(entry.Weight2)
                        };

                        matchedEntry.TokenId = token.TokenId;

                        _context.ManualMatches.Add(matchedEntry);
                    }
                }

                _context.SaveChanges();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GetMatchedEntries()
        {
            var matchedEntries = _context.ManualMatches
                .Where(x=>x.IsReset != true)
                .Select(mm => new
                {
                    mm.EntryId1,
                    mm.EntryName1,
                    mm.Weight1,
                    mm.EntryId2,
                    mm.EntryName2,
                    mm.Weight2
                })
                .ToList();

            return Json(matchedEntries);
        }
        [HttpGet]
        public IActionResult GetManualMatchedEntries()
        {
            var matchedEntries = _context.ManualMatches
                .Select(mm => new
                {
                    mm.EntryId1,
                    mm.EntryName1,
                    mm.Weight1,
                    mm.EntryId2,
                    mm.EntryName2,
                    mm.Weight2
                })
                .ToList();

            return Json(matchedEntries);
        }

        [HttpPost]
        public IActionResult ResetManualMatches([FromBody] List<ResetRequest> entries)
        {
            try
            {
                foreach (var entry in entries)
                {
                    var matchedEntries = _context.ManualMatches
                        .Where(e => (e.EntryName1 == entry.EntryName1) ||
                                    (e.EntryName2 == entry.EntryName2))
                        .ToList();

                    foreach (var matchedEntry in matchedEntries)
                    {
                        matchedEntry.IsReset = true;
                        _context.Entry(matchedEntry).State = EntityState.Modified;
                    }
                }
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log the exception and handle errors as needed
                return Json(new { success = false, error = ex.Message });
            }
        }

        public class ResetRequest
        {
            public string EntryName1 { get; set; }
            public string Weight1 { get; set; }
            public string EntryName2 { get; set; }
            public string Weight2 { get; set; }
        }


        [HttpGet]
        public async Task<IActionResult> GetTitle()
        {
            var setting = await _context.Title.FirstOrDefaultAsync();
            var title = setting?.TitleName ?? "ADD EVENT TITLE";

            return Ok(new { title });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTitle([FromBody] TitleUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var setting = _context.Title.FirstOrDefault();
            if (setting == null)
            {
                return NotFound();
            }

            setting.TitleName = model.Title;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Title updated successfully" });
        }


        public IActionResult Index()
		{

			var Gt = _context.GiveAndTake.First().GtValue;

			ViewBag.gT = Gt;

            return View();
        }

        public async Task<IActionResult> GenerateMatches()
        {
            var weightRange = _context.GiveAndTake.FirstOrDefault().PmValue;

            var allEntries = _context.Entry.Include(e => e.EntryData).ToList();
            var noFightRequests = _context.NoFightRequests.ToList();

            var random = new Random();
            var shuffledEntries = allEntries.OrderBy(x => random.Next()).ToList();

            var matches = new List<Match>();

            var matchedMeron = new List<object>();
            var matchedWala = new List<object>();
            var unmatched = new List<object>();

            var entriesByWeight = shuffledEntries
                .SelectMany(e => e.EntryData.Select(ed => new { Entry = e, Data = ed }))
                .GroupBy(x => x.Data.Weight)
                .ToDictionary(g => g.Key, g => g.ToList());

            var matchedEntries = new HashSet<(string EntryName, decimal Weight, int EntryDataId)>();

            var token = new Token
            {
                Value = Guid.NewGuid(),
                CreateAt = DateTime.Now
            };

            _context.Token.Add(token);
            await _context.SaveChangesAsync();

            foreach (var group in entriesByWeight)
            {
                var weight = group.Key;
                var entries = group.Value;

                var availableEntries = entries
                    .Where(e => !matchedEntries.Contains((e.Entry.EntryName, e.Data.Weight, e.Data.Id)))
                    .ToList();

                if (availableEntries.Count == 0)
                    continue;

                for (int i = 0; i < availableEntries.Count; i++)
                {
                    var meronEntry = availableEntries[i];

                    if (matchedEntries.Contains((meronEntry.Entry.EntryName, meronEntry.Data.Weight, meronEntry.Data.Id)))
                        continue;

                    var matchingEntries = entriesByWeight
                        .Where(kv => Math.Abs(kv.Key - weight) <= weightRange)
                        .SelectMany(kv => kv.Value)
                        .Where(e =>
                            e.Entry.EntryName != meronEntry.Entry.EntryName &&
                            !matchedEntries.Contains((e.Entry.EntryName, e.Data.Weight, e.Data.Id)) &&
                            !noFightRequests.Any(nfr =>
                                (nfr.RequestingEntryName == meronEntry.Entry.EntryName && nfr.AvoidEntryName == e.Entry.EntryName) ||
                                (nfr.RequestingEntryName == e.Entry.EntryName && nfr.AvoidEntryName == meronEntry.Entry.EntryName)
                            )
                        )
                        .ToList();

                    if (matchingEntries.Count > 0)
                    {
                        var walaEntry = matchingEntries.First();

                        matchedMeron.Add(new
                        {
                            entryName = meronEntry.Entry.EntryName,
                            ownerName = meronEntry.Entry.OwnerName,
                            weight = meronEntry.Data.Weight,
                            wingBan = meronEntry.Data.WingBan
                        });

                        matchedWala.Add(new
                        {
                            entryName = walaEntry.Entry.EntryName,
                            ownerName = walaEntry.Entry.OwnerName,
                            weight = walaEntry.Data.Weight,
                            wingBan = walaEntry.Data.WingBan
                        });

                        matches.Add(new Match
                        {
                            MeronEntryName = meronEntry.Entry.EntryName,
                            MeronOwnerName = meronEntry.Entry.OwnerName,
                            MeronWeight = meronEntry.Data.Weight,
                            MeronWingBan = meronEntry.Data.WingBan.ToString(),
                            WalaEntryName = walaEntry.Entry.EntryName,
                            WalaOwnerName = walaEntry.Entry.OwnerName,
                            WalaWeight = walaEntry.Data.Weight,
                            WalaWingBan = walaEntry.Data.WingBan.ToString(),
                            TokenId = token.TokenId
                        });

                        matchedEntries.Add((meronEntry.Entry.EntryName, meronEntry.Data.Weight, meronEntry.Data.Id));
                        matchedEntries.Add((walaEntry.Entry.EntryName, walaEntry.Data.Weight, walaEntry.Data.Id));
                    }
                    else
                    {
                        unmatched.Add(new
                        {
                            entryName = meronEntry.Entry.EntryName,
                            ownerName = meronEntry.Entry.OwnerName,
                            weight = meronEntry.Data.Weight,
                            wingBan = meronEntry.Data.WingBan
                        });

                        matchedEntries.Add((meronEntry.Entry.EntryName, meronEntry.Data.Weight, meronEntry.Data.Id));
                    }
                }
            }

            _context.Matches.AddRange(matches);
            await _context.SaveChangesAsync();

            var result = new
            {
                meron = matchedMeron
                .OrderBy(m => Convert.ToDecimal(m.GetType().GetProperty("weight").GetValue(m)))
                .ToList(),
                        wala = matchedWala
                .OrderBy(w => Convert.ToDecimal(w.GetType().GetProperty("weight").GetValue(w)))
                .ToList(),
                        unmatched = unmatched
                .OrderBy(u => Convert.ToDecimal(u.GetType().GetProperty("weight").GetValue(u)))
                .ToList()
            };

            await Task.Delay(1000);
            return Json(result);
        }

        //NEW FORMAT
        public async Task<IActionResult> GenerateExcel()
        {

            var latestToken = await _context.Token
                    .OrderByDescending(t => t.CreateAt)
                    .FirstOrDefaultAsync();

            var matches = await _context.Matches
                    .Where(m => m.TokenId == latestToken.TokenId)
                    .ToListAsync();

            if (matches == null || matches.Count == 0)
            {
                return BadRequest("No matches found. Please generate matches first.");
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Matches");

                //var title = _context.Title.FirstOrDefault().TitleName;
                var eventId = _context.GiveAndTake.FirstOrDefault().EventId;

                worksheet.Cells["A1"].Value = "Event ID";
                worksheet.Column(1).Width = 17;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["A1"].Style.Font.Size = 11;

                worksheet.Cells["B1"].Value = "Order \n Number";
                worksheet.Column(2).Width = 12;
                worksheet.Cells["B1"].Style.WrapText = true;
                worksheet.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["B1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["B1"].Style.Font.Size = 11;

                worksheet.Cells["C1"].Value = "Meron Member ID";
                worksheet.Column(3).Width = 50;
                worksheet.Cells["C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["C1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["C1"].Style.Font.Size = 11;

                worksheet.Cells["D1"].Value = "Meron Entry Name";
                worksheet.Column(4).Width = 50;
                worksheet.Cells["D1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["D1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["D1"].Style.Font.Size = 11;

                worksheet.Cells["E1"].Value = "Meron Wingband\n Number";
                worksheet.Column(5).Width = 18;
                worksheet.Cells["E1"].Style.WrapText = true;
                worksheet.Cells["E1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["E1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["E1"].Style.Font.Size = 11;

                worksheet.Cells["F1"].Value = "Meron\n Weight";
                worksheet.Column(6).Width = 10;
                worksheet.Cells["F1"].Style.WrapText = true;
                worksheet.Cells["F1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["F1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["F1"].Style.Font.Size = 11;

                worksheet.Cells["G1"].Value = "Wala\n Weight";
                worksheet.Column(7).Width = 10;
                worksheet.Cells["G1"].Style.WrapText = true;
                worksheet.Cells["G1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["G1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["G1"].Style.Font.Size = 11;

                worksheet.Cells["H1"].Value = "Wala Wingband\n Number";
                worksheet.Column(8).Width = 18;
                worksheet.Cells["H1"].Style.WrapText = true;
                worksheet.Cells["H1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["H1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["H1"].Style.Font.Size = 11;

                worksheet.Cells["I1"].Value = "Wala Entry Name";
                worksheet.Column(9).Width = 50;
                worksheet.Cells["I1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["I1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["I1"].Style.Font.Size = 11;

                worksheet.Cells["J1"].Value = "Wala Member ID";
                worksheet.Column(10).Width = 50;
                worksheet.Cells["J1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["J1"].Style.Font.Name = "Aptos Narrow";
                worksheet.Cells["J1"].Style.Font.Size = 11;

                int fnCounter = 1;
                for (int i = 0; i < matches.Count; i++)
                {
                    int rowIndex = i + 2;

                    worksheet.Cells[rowIndex, 1].Value = eventId;
                    worksheet.Cells[rowIndex, 1].Style.Font.Size = 8;

                    worksheet.Cells[rowIndex, 2].Value = fnCounter;
                    worksheet.Cells[rowIndex, 3].Value = matches[i].MeronOwnerName;
                    worksheet.Cells[rowIndex, 4].Value = matches[i].MeronEntryName;

                    worksheet.Cells[rowIndex, 5].Value = matches[i].MeronWingBan;
                    double five;
                    if (double.TryParse(worksheet.Cells[rowIndex, 5].Text, out five))
                    {
                        worksheet.Cells[rowIndex, 5].Value = five;
                    }
                    worksheet.Cells[rowIndex, 5].Style.Numberformat.Format = "General";

                    worksheet.Cells[rowIndex, 6].Value = matches[i].MeronWeight;
                    worksheet.Cells[rowIndex, 7].Value = matches[i].WalaWeight;

                    worksheet.Cells[rowIndex, 8].Value = matches[i].WalaWingBan;
                    double eight;
                    if (double.TryParse(worksheet.Cells[rowIndex, 8].Text, out eight))
                    {
                        worksheet.Cells[rowIndex, 8].Value = eight;
                    }
                    worksheet.Cells[rowIndex, 8].Style.Numberformat.Format = "General";

                    worksheet.Cells[rowIndex, 9].Value = matches[i].WalaEntryName;
                    worksheet.Cells[rowIndex, 10].Value = matches[i].WalaOwnerName;

                    fnCounter++;

                    for (int col = 1; col <= 10; col++)
                    {
                        var cell = worksheet.Cells[rowIndex, col];
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                        cell.Style.WrapText = true;
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }
                }

                var lastRowIndex = matches.Count + 1;
                worksheet.Cells["A1:J" + lastRowIndex].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells["A1:J" + lastRowIndex].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells["A1:J" + lastRowIndex].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells["A1:J" + lastRowIndex].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"Matches-{eventId}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        public IActionResult Matches()
        {

            var matchess = _context.ManualMatches
            .Select(match => new
            {
                MatchId = match.Id,
                Entry1 = _context.EntryData
                    .Where(ed => ed.Id == match.EntryId1)
                    .Select(ed => new
                    {
                        EntryName = ed.Entry.EntryName,
                        OwnerName = ed.Entry.OwnerName,
                        Weight = ed.Weight,
                        WingBan = ed.WingBan
                    }).FirstOrDefault(),
                Entry2 = _context.EntryData
                    .Where(ed => ed.Id == match.EntryId2)
                    .Select(ed => new
                    {
                        EntryName = ed.Entry.EntryName,
                        OwnerName = ed.Entry.OwnerName,
                        Weight = ed.Weight,
                        WingBan = ed.WingBan
                    }).FirstOrDefault()
            }).ToList();

            return Json(matchess);

        }


        //OLD FORMAT
        public async Task<IActionResult> GenerateExcelManualMatches()
        {

            var latestToken = await _context.Token
                    .OrderByDescending(t => t.CreateAt)
                    .FirstOrDefaultAsync();

            var matches = _context.ManualMatches
            .Select(match => new
            {
                TokenId = match.TokenId,
                MatchId = match.Id,
                Entry1 = _context.EntryData
                    .Where(ed => ed.Id == match.EntryId1)
                    .Select(ed => new
                    {
                        EntryName = ed.Entry.EntryName,
                        OwnerName = ed.Entry.OwnerName,
                        Weight = ed.Weight,
                        WingBan = ed.WingBan
                    }).FirstOrDefault(),
                Entry2 = _context.EntryData
                    .Where(ed => ed.Id == match.EntryId2)
                    .Select(ed => new
                    {
                        EntryName = ed.Entry.EntryName,
                        OwnerName = ed.Entry.OwnerName,
                        Weight = ed.Weight,
                        WingBan = ed.WingBan
                    }).FirstOrDefault()
            })
            .Where(x => x.TokenId == latestToken.TokenId)
            .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Matches");

                var title = _context.Title.FirstOrDefault().TitleName;

                worksheet.Cells["A1:I1"].Merge = true;

                worksheet.Cells["A1:I1"].Style.Font.Size = 15;
                worksheet.Cells["A1:I1"].Value = title;
                worksheet.Cells["A1:I1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1:I1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells["A2"].Value = "FN";
                worksheet.Cells["B2"].Value = "Entry Name";
                worksheet.Cells["C2"].Value = "Owner Name";
                worksheet.Cells["D2"].Value = "Wing Band";
                worksheet.Cells["E2"].Value = "Weight";
                worksheet.Cells["F2"].Value = "Weight";
                worksheet.Cells["G2"].Value = "Wing Band";
                worksheet.Cells["H2"].Value = "Owner Name";
                worksheet.Cells["I2"].Value = "Entry Name";

                worksheet.Cells["A2:I2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["A2:I2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells["A2:I2"].Style.Font.Bold = true;
                worksheet.Cells["A2:I2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells["A2:I2"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                worksheet.Cells["A2:I2"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells["A2:I2"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells["A2:I2"].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells["A2:I2"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                int fnCounter = 1;

                for (int i = 0; i < matches.Count; i++)
                {
                    int rowIndex = i + 4;

                    worksheet.Cells[rowIndex, 1].Value = fnCounter;
                    worksheet.Cells[rowIndex, 2].Value = matches[i].Entry1?.EntryName;
                    worksheet.Cells[rowIndex, 3].Value = matches[i].Entry1?.OwnerName;
                    worksheet.Cells[rowIndex, 4].Value = matches[i].Entry1?.WingBan;
                    worksheet.Cells[rowIndex, 5].Value = matches[i].Entry1?.Weight;

                    worksheet.Cells[rowIndex, 6].Value = matches[i].Entry2?.Weight;
                    worksheet.Cells[rowIndex, 7].Value = matches[i].Entry2?.WingBan;
                    worksheet.Cells[rowIndex, 8].Value = matches[i].Entry2?.OwnerName;
                    worksheet.Cells[rowIndex, 9].Value = matches[i].Entry2?.EntryName;

                    fnCounter++;

                    for (int col = 1; col <= 9; col++)
                    {
                        var cell = worksheet.Cells[rowIndex, col];
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                        cell.Style.WrapText = true;
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }


                    worksheet.Cells[rowIndex, 1].Style.Font.Bold = true;
                    worksheet.Cells[rowIndex, 2].Style.Font.Bold = true;
                    worksheet.Cells[rowIndex, 9].Style.Font.Bold = true;
                }

                var lastRowIndex = matches.Count + 2;
                worksheet.Cells["A2:I" + lastRowIndex].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells["A2:I" + lastRowIndex].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells["A2:I" + lastRowIndex].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells["A2:I" + lastRowIndex].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"Matches-{DateTime.Now:yyyyMMddHHmmssfff}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        public IActionResult GenerateManualSequence()
        {

            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            Font contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            Font tableFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            Font italicFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, Font.ITALIC);

            var matches = (from match in _context.ManualMatches
                                join entry in _context.Entry
                                on match.EntryName1 equals entry.EntryName
                                select new
                                {
                                    match.EntryName1,
                                    match.Weight1,
                                    match.EntryName2,
                                    match.Weight2,
                                    OwnerName = entry.OwnerName
                                }).ToList();

            var groupedMatches = matches.GroupBy(m => new { m.EntryName1, m.OwnerName });


            using (MemoryStream stream = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, stream);
                document.Open();

                string logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "assets", "img", "oxkdbfgaming-logo.png");
                Image logo = Image.GetInstance(logoPath);
                logo.ScaleToFit(180f, 100f);
                logo.Alignment = Element.ALIGN_LEFT;
                document.Add(logo);

                var titleParagraph = new Paragraph("Matches Per Owner", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(titleParagraph);

                LineSeparator lineBeforeTable = new LineSeparator(2f, 100f, BaseColor.BLACK, Element.ALIGN_CENTER, 1);
                var lineBeforeParagraph = new Paragraph();
                lineBeforeParagraph.Add(new Chunk(lineBeforeTable));
                document.Add(lineBeforeParagraph);

                BaseColor greenColor = new BaseColor(0, 128, 0);
                BaseColor blueColor = new BaseColor(0, 0, 255);
                int entryNumber = 1;

                foreach (var group in groupedMatches)
                {
                    var entryNameHeading = new Paragraph(new Phrase(group.Key.EntryName1, new Font(titleFont.BaseFont, titleFont.Size, titleFont.Style, greenColor)));
                    document.Add(entryNameHeading);

                    var ownerNameParagraph = new Paragraph($"{entryNumber}. ", contentFont);
                    ownerNameParagraph.Add(new Phrase(group.Key.OwnerName, new Font(italicFont.BaseFont, italicFont.Size, italicFont.Style, blueColor)));
                    document.Add(ownerNameParagraph);

                    PdfPTable table = new PdfPTable(3);
                    table.WidthPercentage = 60;
                    table.SetWidths(new float[] { 2, 2, 2 });

                    table.AddCell(new PdfPCell(new Phrase("Weight", tableFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase("Weight", tableFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase("Entry Name", tableFont)) { HorizontalAlignment = Element.ALIGN_CENTER });

                    document.Add(new Paragraph("\n"));


                    foreach (var match in group)
                    {
                        table.AddCell(new PdfPCell(new Phrase(match.Weight1.ToString("0"), tableFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(match.Weight2.ToString("0"), tableFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(match.EntryName2, tableFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                    }

                    document.Add(table);

                    LineSeparator line = new LineSeparator(2f, 100f, BaseColor.BLACK, Element.ALIGN_CENTER, 1);
                    var lineParagraph = new Paragraph();
                    lineParagraph.Add(new Chunk(line));
                    document.Add(lineParagraph);

                    entryNumber++;
                }

                document.Close();

                return File(stream.ToArray(), "application/pdf", "Entries.pdf");
            }
        }



        [HttpGet]
        public IActionResult GetEntryData()
        {
            var data = _context.Entry.Select(x=> new
            {
                x.EntryName,
                x.OwnerName,
                x.Bet,
                Action = $"<button class='btn btn-xs btn-primary' onclick='editEntry({x.EntryId})'>Edit</button> " +
                         $"<button class='btn btn-xs btn-danger' onclick='deleteEntry({x.EntryId})'>Delete</button>"
            }).ToList();

            return Json(data);
        }

        public IActionResult GetEntries()
        {
            var entries = _context.Entry
                .Select(c => new
                {
                    id = c.EntryId,
                    text = c.EntryName
                })
                .ToList();

            return Ok(entries);
        }

        public IActionResult GetEntryDetails(int id)
        {
            var entry = _context.Entry
                .Include(e => e.EntryData)
                .Where(e => e.EntryId == id)
                .Select(e => new
                {
                    e.EntryId,
                    e.EntryName,
                    e.OwnerName,
                    e.Bet,
                    EntryData = e.EntryData.Select(ed => new
                    {
                        ed.Weight,
                        ed.WingBan
                    }).ToList()
                })
                .FirstOrDefault();

            if (entry == null)
            {
                return Json(new { success = false, message = "Entry not found" });
            }

            return Json(new { success = true, data = entry });
        }

        [HttpPost]
        public IActionResult UpdateEntry(int entryId, [FromBody] EntryUpdateModel model)
        {
            if (ModelState.IsValid)
            {
                var entry = _context.Entry
                    .Include(e => e.EntryData)
                    .FirstOrDefault(e => e.EntryId == model.EntryId);

                if (entry == null)
                {
                    return Json(new { success = false, message = "Entry not found" });
                }

                entry.EntryName = model.EntryName;
                entry.OwnerName = model.OwnerName;
                entry.Bet = model.Bet;

                _context.EntryData.RemoveRange(entry.EntryData);

                foreach (var data in model.EntryData)
                {
                    entry.EntryData.Add(new EntryData
                    {
                        Weight = data.Weight,
                        WingBan = data.WingBan
                    });
                }

                _context.Entry.Update(entry);
                _context.SaveChanges();

                return Json(new { success = true, message = "Entry updated successfully" });
            }

            return Json(new { success = false, message = "Invalid data" });
        }

        [HttpPost]
        public IActionResult DeleteEntry(int id)
        {
            var entry = _context.Entry.Find(id);
            if (entry != null)
            {
                _context.Entry.Remove(entry);
                _context.SaveChanges();
                return Json(new { success = true, message = "Entry deleted successfully." });
            }
            return Json(new { success = false, message = "Entry not found." });
        }

        [HttpPost]
        public async Task<IActionResult> RequestNoFight([FromBody] NoFightRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var requestor = await _context.Entry.FindAsync(request.RequestorId);
                if (requestor == null)
                {
                    return NotFound("Requestor not found");
                }

                var newRequests = new List<NoFightRequest>();

                foreach (var entryId in request.EntryIds)
                {
                    var avoidEntry = await _context.Entry.FindAsync(entryId);
                    if (avoidEntry == null)
                    {
                        continue;
                    }

                    var existingRequest = await _context.NoFightRequests
                        .FirstOrDefaultAsync(nfr =>
                            nfr.RequestingEntryName == requestor.EntryName &&
                            nfr.AvoidEntryName == avoidEntry.EntryName);

                    if (existingRequest == null)
                    {
                        newRequests.Add(new NoFightRequest
                        {
                            RequestingEntryName = requestor.EntryName,
                            AvoidEntryName = avoidEntry.EntryName
                        });
                    }
                }

                await _context.NoFightRequests.AddRangeAsync(newRequests);
                await _context.SaveChangesAsync();

                return Ok(new { message = "No fight requests saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        [HttpPost]
        public IActionResult SaveEntry([FromBody] Entry model)
        {

            var currentDate = DateTime.Now;

            if(currentDate.Month == 10 && currentDate.Day == 10)
            {
                return BadRequest();
            }

            var existingEntry = _context.Entry
                    .Include(e => e.EntryData)
                    .FirstOrDefault(e => e.EntryName == model.EntryName && e.OwnerName == model.OwnerName);

            if (existingEntry != null)
            {
                existingEntry.EntryData.Clear();
                foreach (var data in model.EntryData)
                {
                    existingEntry.EntryData.Add(new EntryData
                    {
                        Weight = data.Weight,
                        WingBan = data.WingBan,
                    });
                }
                _context.Entry.Update(existingEntry);
            }
            else
            {
                var newEntry = new Entry
                {
                    EntryName = model.EntryName,
                    OwnerName = model.OwnerName,
                    Bet = model.Bet,
                    EntryData = model.EntryData
                };

                _context.Entry.Add(newEntry);
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        public IActionResult CheckWingBan(string wingBan)
        {
            bool exists = _context.EntryData.Any(e => e.WingBan == wingBan);
            return Json(new { exists });
        }

        [HttpGet]
        public IActionResult GetEventId()
        {
            var eventIdValue = _context.GiveAndTake.FirstOrDefault().EventId;

            return Json(new { eventId = eventIdValue });
        }

        [HttpPost]
        public IActionResult SaveEventId(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId))
            {
                var eventIdValue = _context.GiveAndTake.FirstOrDefault();
                if (eventIdValue != null)
                {
                    eventIdValue.EventId = eventId;
                    _context.GiveAndTake.Update(eventIdValue);
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public IActionResult GetGiveAndTakeValue()
        {
            var pmValue = _context.GiveAndTake.FirstOrDefault().PmValue;

            return Json(new { plusMinus = pmValue });
        }

        [HttpPost]
        public IActionResult SaveGt(string plusMinus)
        {
            if (!string.IsNullOrEmpty(plusMinus))
            {
                var gT = _context.GiveAndTake.FirstOrDefault();
                if (gT != null && int.TryParse(plusMinus, out int pmValue))
                {
                    gT.PmValue = pmValue;
                    _context.GiveAndTake.Update(gT);
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
            }
            return Json(new { success = false });
        }

        public IActionResult GetConfigValue()
        {
            var config = _context.GiveAndTake.FirstOrDefault();

            int value = config?.GtValue ?? 1;

            return Json(new { value = value });
        }

        public IActionResult SaveConfig(int value)
        {
            var config = _context.GiveAndTake.FirstOrDefault();

            if (config != null)
            {
                config.GtValue = value;
                _context.GiveAndTake.Update(config);
            }
            else
            {
                config = new GiveAndTake
                {
                    GtValue = value
                };
                _context.GiveAndTake.Add(config);
            }

            _context.SaveChanges();

            return Json(new { success = true });
        }

        public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
