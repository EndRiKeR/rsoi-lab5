using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Common.DtoModels.ErrorDto;
using Common.DtoModels.TicketsServiceDto;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Common.DtoModels.FlightServiceDto;
using Microsoft.AspNetCore.Authorization;
using TicketsService.Database.Enums;
using TicketsService.Database.Models;
using TicketsService.Database.Repositories.Interfaces;

namespace TicketsService.Controllers
{
    [ApiController]
    [Route("api/v1/tickets")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly HttpClient _gatewayClient;
        
        public TicketsController(ITicketRepository ticketRepository, IHttpClientFactory httpClientFactory)
        {
            _ticketRepository = ticketRepository;
            _gatewayClient = httpClientFactory.CreateClient("Gateway");
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUserTickets()
        {
            try
            {
                var username = GetUsernameFromToken();
                var allTickets = await _ticketRepository.GetAll();
                var userTickets = allTickets.Where(t => t.Username == username).ToList();

                List<TicketResponse> ticketResponses = new();
                foreach (var userTicket in userTickets)
                {
                    var flightData = await GetFlightByNumber(userTicket.FlightNumber, Request.Headers["Authorization"].FirstOrDefault());
                    ticketResponses.Add(new TicketResponse
                    {
                        TicketUid = userTicket.TicketUid,
                        FlightNumber = userTicket.FlightNumber,
                        FromAirport = flightData.FromAirport,
                        ToAirport = flightData.ToAirport,
                        Date = DateTime.Now,
                        Price = userTicket.Price,
                        Status = userTicket.Status.ToString()
                    });
                }
                
                return Ok(ticketResponses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpGet("{ticketUid}")]
        public async Task<IActionResult> GetTicket(Guid ticketUid)
        {
            try
            {
                var username = GetUsernameFromToken();
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                
                var allTickets = await _ticketRepository.GetAll();
                var ticket = allTickets.FirstOrDefault(t => t.TicketUid == ticketUid && t.Username == username);
                
                if (ticket == null)
                    return NotFound(new ErrorResponse { Message = "Ticket not found" });
                
                var flightData = await GetFlightByNumber(ticket.FlightNumber, authHeader);
                
                var response = new TicketResponse
                {
                    TicketUid = ticket.TicketUid,
                    FlightNumber = ticket.FlightNumber,
                    FromAirport = flightData.FromAirport,
                    ToAirport = flightData.ToAirport,
                    Date = DateTime.Now,
                    Price = ticket.Price,
                    Status = ticket.Status.ToString()
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> AddTicket([FromBody] TicketAddRequest request)
        {
            try
            {
                var ticket = new Ticket
                {
                    TicketUid = request.TicketUid,
                    Username = request.Username,
                    FlightNumber = request.FlightNumber,
                    Price = request.Price,
                    Status = TicketStatusConverter.ToStatus(request.Status)
                };

                await _ticketRepository.Add(ticket);
                
                return Ok(new TicketAddResponse { TicketUid = ticket.TicketUid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpDelete]
        public async Task<IActionResult> DeleteTicket([FromBody] TicketDeleteRequest request)
        {
            try
            {
                await _ticketRepository.DeleteByTicketUid(request.TicketUid);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpDelete("{ticketUid}")]
        public async Task<IActionResult> ReturnTicket(Guid ticketUid)
        {
            try
            {
                var username = GetUsernameFromToken();
                
                var usernameValue = username.ToString();
                var allTickets = await _ticketRepository.GetAll();
                var ticket = allTickets.FirstOrDefault(t => t.TicketUid == ticketUid && t.Username == usernameValue);
                
                if (ticket == null)
                    return NotFound(new ErrorResponse { Message = "Ticket not found" });
                
                ticket.Status = TicketStatusConverter.ToStatus("CANCELED");
                await _ticketRepository.Update(ticket);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        private async Task<FlightResponse> GetFlightByNumber(string flightNumber, string token)
        {
            var flightRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/flights/{flightNumber}");
            flightRequest.Headers.Add("Authorization", token);
            
            var flightResponse = await _gatewayClient.SendAsync(flightRequest);

            if (!flightResponse.IsSuccessStatusCode)
                return null;
            
            var content = await flightResponse.Content.ReadAsStringAsync();
            var flight= JsonSerializer.Deserialize<FlightResponse>(content);
            
            Console.WriteLine(content);
                
            return flight;
        }
        
        private string GetUsernameFromToken()
        {
            var usernameClaim = User.FindFirst("preferred_username") ?? 
                                User.FindFirst(ClaimTypes.Name) ??
                                User.FindFirst(ClaimTypes.NameIdentifier) ?? 
                                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                                User.FindFirst("email") ??
                                User.FindFirst("upn") ??
                                User.FindFirst("sub");
    
            if (usernameClaim == null)
                throw new UnauthorizedAccessException("User not found in token claims");
    
            return usernameClaim.Value;
        }
    }
}