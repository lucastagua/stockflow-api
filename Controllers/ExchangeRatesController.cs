using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;
using StockFlow.Api.Models;
using StockFlow.Api.Helpers;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExchangeRatesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExchangeRatesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<ExchangeRate>> GetLatestExchangeRate()
    {
        var latestRate = await _context.ExchangeRates
            .OrderByDescending(e => e.Date)
            .FirstOrDefaultAsync();

        if (latestRate is null)
        {
            return NotFound(new
            {
                message = "No exchange rate has been registered yet."
            });
        }

        return Ok(latestRate);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExchangeRate>>> GetExchangeRateHistory()
    {
        var rates = await _context.ExchangeRates
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        return Ok(rates);
    }

    [HttpPost]
    public async Task<ActionResult<ExchangeRate>> CreateExchangeRate(CreateExchangeRateDto createExchangeRateDto)
    {
        if (createExchangeRateDto.Value <= 0)
        {
            return BadRequest(new
            {
                message = "Exchange rate value must be greater than zero."
            });
        }

        var exchangeRate = new ExchangeRate
        {
            Value = createExchangeRateDto.Value,
            Date = DateTime.UtcNow
        };

        _context.ExchangeRates.Add(exchangeRate);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetLatestExchangeRate),
            new { id = exchangeRate.Id },
            exchangeRate
        );
    }

    [HttpPost("calculate-price")]
    public async Task<ActionResult<CalculatedPriceResponseDto>> CalculateSuggestedPrice(
        CalculatePriceDto calculatePriceDto)
    {
        if (calculatePriceDto.CostUsd <= 0)
        {
            return BadRequest(new
            {
                message = "Cost in USD must be greater than zero."
            });
        }

        if (calculatePriceDto.ProfitMarginPercentage < 0)
        {
            return BadRequest(new
            {
                message = "Profit margin percentage cannot be negative."
            });
        }

        var latestRate = await _context.ExchangeRates
            .OrderByDescending(e => e.Date)
            .FirstOrDefaultAsync();

        if (latestRate is null)
        {
            return BadRequest(new
            {
                message = "No exchange rate has been registered yet."
            });
        }

        var suggestedPrice = PriceCalculator.CalculatePriceArs(
            calculatePriceDto.CostUsd,
            latestRate.Value,
            calculatePriceDto.ProfitMarginPercentage
        );

        var response = new CalculatedPriceResponseDto
        {
            CostUsd = calculatePriceDto.CostUsd,
            ExchangeRate = latestRate.Value,
            ProfitMarginPercentage = calculatePriceDto.ProfitMarginPercentage,
            SuggestedPriceArs = suggestedPrice
        };

        return Ok(response);
    }
}