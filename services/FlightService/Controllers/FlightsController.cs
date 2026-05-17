using Common.DtoModels.ErrorDto;
using Common.DtoModels.FlightServiceDto;
using FlightService.Database.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightService.Controllers
{
    [ApiController]
    [Route("api/v1/flights")]
    [Authorize]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightRepository _flightRepository;
        private readonly IAirportRepository _airportRepository;
        
        public FlightsController(IFlightRepository flightRepository, IAirportRepository airportRepository)
        {
            _flightRepository = flightRepository;
            _airportRepository = airportRepository;
        }
        
        [HttpPost("{flightNumber}/reserve")]
        [Authorize]
        public async Task<IActionResult> ReserveSeat([FromRoute] string flightNumber)
        {
            try
            {
                var flights = await _flightRepository.GetFlightsByFlightNumber(flightNumber);
                var flight = flights[0];
                
                if (flight.AvailableSeats <= 0)
                    return Conflict(new ErrorResponse { Message = "Билетов на этот рейс больше нет." });

                flight.AvailableSeats--;
                await _flightRepository.Update(flight);
                
                return Ok();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Conflict(new ErrorResponse { Message = "Не удалось зарезервировать место из-за одновременного запроса. Попробуйте ещё раз." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpPost("{flightNumber}/release")]
        [Authorize]
        public async Task<IActionResult> ReleaseSeat([FromRoute] string flightNumber)
        {
            try
            {
                var flights = await _flightRepository.GetFlightsByFlightNumber(flightNumber);
                var flight = flights[0];

                flight.AvailableSeats++;
                await _flightRepository.Update(flight);
                
                return Ok();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Conflict(new ErrorResponse { Message = "Не удалось зарезервировать место из-за одновременного запроса. Попробуйте ещё раз." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetFlights([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            try
            {
                var allFlights = await _flightRepository.GetAll();
                
                var totalElements = allFlights.Count;
                var pagedFlights = allFlights
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();
                
                var flightResponses = new List<FlightResponse>();
                foreach (var flight in pagedFlights)
                {
                    string fromAirportName = "Unknown";
                    string toAirportName = "Unknown";
                    
                    if (flight.FromAirportId.HasValue)
                    {
                        var fromAirport = await _airportRepository.GetById(flight.FromAirportId.Value);
                        fromAirportName = $"{fromAirport.City} {fromAirport.Name}";
                    }
                    
                    if (flight.ToAirportId.HasValue)
                    {
                        var toAirport = await _airportRepository.GetById(flight.ToAirportId.Value);
                        toAirportName = $"{toAirport.City} {toAirport.Name}";
                    }
                    
                    flightResponses.Add(new FlightResponse
                    {
                        FlightNumber = flight.FlightNumber,
                        FromAirport = fromAirportName,
                        ToAirport = toAirportName,
                        Date = flight.DateTime,
                        Price = flight.Price,
                        AvailableSeats = flight.AvailableSeats
                    });
                }
                
                var response = new PaginationResponse
                {
                    Page = page,
                    PageSize = size,
                    TotalElements = totalElements,
                    Items = flightResponses
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpGet("{flightNumber}")]
        public async Task<IActionResult> GetFlightByNumber([FromRoute] string flightNumber)
        {
            try
            {
                var flights = await _flightRepository.GetFlightsByFlightNumber(flightNumber);
                var flight = flights.FirstOrDefault();
                
                if (flight == null)
                {
                    return NotFound(new ErrorResponse { Message = $"Flight with number {flightNumber} not found" });
                }
                
                string fromAirportName = "Unknown";
                string toAirportName = "Unknown";
                
                if (flight.FromAirportId.HasValue)
                {
                    var fromAirport = await _airportRepository.GetById(flight.FromAirportId.Value);
                    fromAirportName = $"{fromAirport.City} {fromAirport.Name}";
                }
                
                if (flight.ToAirportId.HasValue)
                {
                    var toAirport = await _airportRepository.GetById(flight.ToAirportId.Value);
                    toAirportName = $"{toAirport.City} {toAirport.Name}";
                }
                
                var response = new FlightResponse
                {
                    FlightNumber = flight.FlightNumber,
                    FromAirport = fromAirportName,
                    ToAirport = toAirportName,
                    Date = flight.DateTime,
                    Price = flight.Price,
                    AvailableSeats = flight.AvailableSeats
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
    }
}