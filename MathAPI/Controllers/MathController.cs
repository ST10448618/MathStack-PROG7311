    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using MathAPI.Models;
    using System.IO;

namespace MathsAppMvc.Controllers
{

[Route("api/[controller]")]
[ApiController]
public class MathController : Controller
{
    private readonly MathDbContext _context;

    public MathController(MathDbContext context)
    {
        _context = context;
    }

    public IActionResult Calculate()
{
    List<SelectListItem> operations = new List<SelectListItem> {
        new SelectListItem { Value = "1", Text = "+" },
        new SelectListItem { Value = "2", Text = "-" },
        new SelectListItem { Value = "3", Text = "**" },
        new SelectListItem { Value = "4", Text = "/" },

        };

    ViewBag.Operations = operations;

    return View();
}

[HttpPost("PostCalculate")]
public async Task<IActionResult> PostCalculate(MathCalculation mathCalculation)
{
    // Token check
    if (mathCalculation.FirebaseUuid == null || mathCalculation.FirebaseUuid == "")
    {
        return Unauthorized(new Error("Token missing!"));
    }

    // Check if equation is complete
    if (mathCalculation.FirstNumber == null || mathCalculation.SecondNumber == null || mathCalculation.Operation == 0)
    {
        return BadRequest(new Error("Math equation not complete!"));
    }

    decimal? Result = 0;

    // Create calculation using factory
    try
    {
        mathCalculation = MathCalculation.Create(mathCalculation.FirstNumber, mathCalculation.SecondNumber, mathCalculation.Operation, Result, mathCalculation.FirebaseUuid);
    }
    catch (Exception ex)
    {
        return Created(mathCalculation.CalculationId.ToString(), mathCalculation);
    }

    // Perform calculation
    switch (mathCalculation.Operation)
    {
        case 1:
            mathCalculation.Result = mathCalculation.FirstNumber + mathCalculation.SecondNumber;
            break;

        case 2:
            mathCalculation.Result = mathCalculation.FirstNumber - mathCalculation.SecondNumber;
            break;

        case 3:
            mathCalculation.Result = mathCalculation.FirstNumber * mathCalculation.SecondNumber;
            break;

        default:
            mathCalculation.Result = mathCalculation.FirstNumber / mathCalculation.SecondNumber;
            break;
    } // 6 syringa Drive 

    // Save to database
    _context.Add(mathCalculation);
    await _context.SaveChangesAsync();

    // Return JSON response
    return Created(mathCalculation.CalculationId.ToString(), mathCalculation);
}

[HttpGet("GetHistory")]
public async Task<IActionResult> GetHistory(string Token)
{

    if (Token == null)
    {
        return Unauthorized(new Error("Token missing!"));
    }

    List<MathCalculation> historyItems = await _context.MathCalculations.Where(m => m.FirebaseUuid.Equals(Token)).ToListAsync();

        if (historyItems.Count > 0)
    {
        return Ok(historyItems);
    } else
    {
        return NotFound(new Error("No history found!"));
    }
}

[HttpGet("DeleteHistory")]
public async Task<IActionResult> DeleteHistory(string Token)
{

    if (Token == null)
    {
        return Unauthorized(new Error("Token missing!"));
    }

    List<MathCalculation> removableItems = await _context.MathCalculations.Where(m => m.FirebaseUuid.Equals(Token)).ToListAsync();

    _context.MathCalculations.RemoveRange(_context.MathCalculations.Where(m => m.FirebaseUuid.Equals(Token)));
    _context.SaveChangesAsync();

        if (removableItems.Count > 0)
    {
        _context.MathCalculations.RemoveRange(removableItems);
        await _context.SaveChangesAsync();
        return Ok(removableItems);
    }
    else
    {
        return NotFound(new Error("No history found!"));
    }
        }
}

}
