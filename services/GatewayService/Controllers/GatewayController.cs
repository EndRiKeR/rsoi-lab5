using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Common.CircuitBreaker;
using Common.CircuitBreaker.Enums;
using Common.DtoModels.BonusServiceDto;
using Common.DtoModels.ErrorDto;
using Common.DtoModels.FlightServiceDto;
using Common.DtoModels.GatewayDto;
using Common.DtoModels.TicketsServiceDto;
using Common.Errors;
using Common.Fallbacks;
using Common.RetryQueue;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace GatewayService.Controllers
{
    [ApiController]
    [Route("api/v1")]
    [Authorize]
    public class GatewayController : ControllerBase
    {
        private readonly HttpClient _ticketsClient;
        private readonly HttpClient _flightsClient;
        private readonly HttpClient _privilegeClient;
        private readonly HttpClient _statisticsClient;
        private readonly CircuitBreakersController _circuitBreakersController;
        private readonly ControllersFallbacks _fallbacks;
        private readonly RetryQueueService _queueService;
        private readonly IProducer<Null, string> _producer;
        private readonly ILogger<GatewayController> _logger;
        
        public GatewayController(
            IHttpClientFactory httpClientFactory,
            CircuitBreakersController circuitBreakersController,
            ControllersFallbacks fallbacks,
            RetryQueueService queueService,
            IProducer<Null, string> producer,
            ILogger<GatewayController> logger)
        {
            _privilegeClient = httpClientFactory.CreateClient("BonusService");
            _flightsClient = httpClientFactory.CreateClient("FlightService");
            _ticketsClient = httpClientFactory.CreateClient("TicketsService");
            _statisticsClient = httpClientFactory.CreateClient("StatisticService");
            
            _circuitBreakersController = circuitBreakersController;
            _fallbacks = fallbacks;
            _queueService = queueService;
            _producer = producer;
            _logger = logger;
        }
        
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var relativeUri = new Uri("/api/v1/statistics", UriKind.Relative);
                if (from.HasValue || to.HasValue)
                {
                    var query = new List<string>();
                    if (from.HasValue) query.Add($"from={from.Value:O}");
                    if (to.HasValue) query.Add($"to={to.Value:O}");
                    relativeUri = new Uri($"/api/v1/statistics?{string.Join("&", query)}", UriKind.Relative);
                }

                var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                    request.Headers.Add("Authorization", authHeader);

                var response = await _statisticsClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, content);

                var json = JsonSerializer.Deserialize<List<object>>(content);
                return Ok(json ?? new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching statistics");
                return StatusCode(500, new { message = ex.Message });
            }
        }
        
        [HttpGet("flights")]
        public async Task<IActionResult> GetFlights([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/flights");
                if (!string.IsNullOrEmpty(authHeader))
                    request.Headers.Add("Authorization", authHeader);
                PaginationResponse? response = await _circuitBreakersController.ExecuteAsync(
                    Services.Flight,
                    async () => await SendRequest<PaginationResponse>(_flightsClient, request)
                );

                return Ok(response);
            }
            catch (ServerDiedException ex)
            {
                return StatusCode(503, new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpGet("flights/{flightNumber}")]
        public async Task<IActionResult> GetFlights([FromRoute] string flightNumber)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/flights/{flightNumber}");
                
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                    request.Headers.Add("Authorization", authHeader);
                
                FlightResponse? response = await _circuitBreakersController.ExecuteSoftAsync(
                    Services.Flight,
                    async () => await SendRequest<FlightResponse>(_flightsClient, request),
                    () => _fallbacks.GetFlightDataFallback(flightNumber)
                );
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                if (ex is ServerDiedException)
                    return StatusCode(503, new ErrorResponse { Message = ex.Message });
                
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpGet("tickets")]
        public async Task<IActionResult> GetUserTickets()
        {
            try
            {
                GetUsernameFromToken();
                
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tickets");
                
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                    request.Headers.Add("Authorization", authHeader);
                
                List<TicketResponse>? response = await _circuitBreakersController.ExecuteSoftAsync(
                    Services.Ticket,
                    async () => await SendRequest<List<TicketResponse>>(_ticketsClient, request),
                    () => _fallbacks.GetAllTicketsFallback()
                );
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                if (ex is ServerDiedException)
                    return StatusCode(503, new ErrorResponse { Message = ex.Message });
                
                if (ex is UnauthorizedAccessException)
                    return StatusCode(401, new ErrorResponse { Message = ex.Message });
                
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpGet("tickets/{ticketUid}")]
        public async Task<IActionResult> GetTicket(Guid ticketUid)
        {
            try
            {
                GetUsernameFromToken();
                
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/tickets/{ticketUid}");
                
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                    request.Headers.Add("Authorization", authHeader);
                
                TicketResponse? response = await _circuitBreakersController.ExecuteSoftAsync(
                    Services.Ticket,
                    async () => await SendRequest<TicketResponse>(_ticketsClient, request),
                    () => _fallbacks.GetTicketsFallback(ticketUid)
                );
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                if (ex is ServerDiedException)
                    return StatusCode(503, new ErrorResponse { Message = ex.Message });
                
                if (ex is UnauthorizedAccessException)
                    return StatusCode(401, new ErrorResponse { Message = ex.Message });
                
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpPost("tickets")]
        public async Task<IActionResult> BuyTicket([FromBody] TicketPurchaseRequest requestDto)
        {
            bool isTicketAdded = false;
            Guid ticketUid = Guid.NewGuid();
            TicketPurchaseResponse response = null;
            
            try
            {
                var username = GetUsernameFromToken();
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                
                // запрос на полет
                // если не найден - ошибка
                var flightRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/flights/{requestDto.FlightNumber}");
                if (!string.IsNullOrEmpty(authHeader))
                    flightRequest.Headers.Add("Authorization", authHeader);
                
                FlightResponse? flightResponse = await _circuitBreakersController.ExecuteAsync(
                    Services.Flight,
                    async () => await SendRequest<FlightResponse>(_flightsClient, flightRequest)
                );
                
                // -1 билет
                var reserveRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/flights/{requestDto.FlightNumber}/reserve");
                if (!string.IsNullOrEmpty(authHeader))
                    reserveRequest.Headers.Add("Authorization", authHeader);

                var reserveResponse = await _flightsClient.SendAsync(reserveRequest);
                if (!reserveResponse.IsSuccessStatusCode)
                {
                    var reserveError = await reserveResponse.Content.ReadAsStringAsync();
                    return StatusCode((int)reserveResponse.StatusCode, reserveError);
                }

                try
                {
                    // пытаемся создать запись о новом билете
                    // нет - ошибка
                    var addTicketRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/tickets/")
                    {
                        Content = JsonContent.Create(new TicketAddRequest
                        {
                            TicketUid = ticketUid,
                            FlightNumber = requestDto.FlightNumber,
                            Username = username,
                            Price = requestDto.Price,
                            Status = "PAID"
                        })
                    };
                    if (!string.IsNullOrEmpty(authHeader))
                        addTicketRequest.Headers.Add("Authorization", authHeader);
                    
                    TicketAddResponse? ticketResponse = await _circuitBreakersController.ExecuteAsync(
                        Services.Ticket,
                        async () => await SendRequest<TicketAddResponse>(_ticketsClient, addTicketRequest)
                    );

                    isTicketAdded = true;
                    
                    // если оплачиваем бонусами - запросик к бонусам
                    // нет - ошибка + удаляем добавленный билет
                    var (paidByBonuses, paidByMoney) = await CalculatePayment(requestDto.Price, requestDto.PaidFromBalance, username);
                    var privilegeInfo = await UpdateBonusBalance(username, ticketUid, paidByBonuses, requestDto.Price);
                    
                    response = new TicketPurchaseResponse
                    {
                        TicketUid = ticketUid,
                        FlightNumber = requestDto.FlightNumber,
                        FromAirport = flightResponse.FromAirport,
                        ToAirport = flightResponse.ToAirport, 
                        Date = DateTime.Now,
                        Price = requestDto.Price,
                        PaidByMoney = paidByMoney,
                        PaidByBonuses = paidByBonuses,
                        Status = "PAID",
                        Privilege = privilegeInfo
                    };
                    
                    var message = new Message<Null, string>
                    {
                        Value = JsonSerializer.Serialize(new
                        {
                            EventType = "TicketPurchased",
                            Timestamp = DateTime.UtcNow,
                            Username = username,
                            Payload = JsonSerializer.Serialize(new { requestDto.FlightNumber, requestDto.Price })
                        })
                    };
                    
                    var deliveryResult = await _producer.ProduceAsync("rsoi-events", message, CancellationToken.None)
                        .WaitAsync(TimeSpan.FromSeconds(120));
                    
                    return Ok(response);
                }
                catch (Exception)
                {
                    // откатываем резерв при ошибке
                    var releaseRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/flights/{requestDto.FlightNumber}/release");
                    if (!string.IsNullOrEmpty(authHeader))
                        releaseRequest.Headers.Add("Authorization", authHeader);
                    await _flightsClient.SendAsync(releaseRequest);

                    throw;
                }
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Kafka timeout, but ticket was purchased");
                return Ok(response);
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "Failed to produce Kafka message");
                return Ok(response);
            }
            catch (Exception ex)
            {
                if (isTicketAdded)
                {
                    var addTicketRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/tickets/{ticketUid}");
                    await _ticketsClient.SendAsync(addTicketRequest);
                }
                
                if (ex is ServerDiedException)
                    return StatusCode(503, new ErrorResponse { Message = ex.Message });
                
                if (ex is UnauthorizedAccessException)
                    return StatusCode(401, new ErrorResponse { Message = ex.Message });
                
                return StatusCode(503, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpDelete("tickets/{ticketUid}")]
        public async Task<IActionResult> ReturnTicket(Guid ticketUid)
        {
            try
            {
                var username = GetUsernameFromToken();
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                var token = authHeader?.Replace("Bearer ", "");

                var ticketInfoRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/tickets/{ticketUid}");
                if (!string.IsNullOrEmpty(authHeader))
                    ticketInfoRequest.Headers.Add("Authorization", authHeader);
                var ticketInfoResponse = await _ticketsClient.SendAsync(ticketInfoRequest);
                var ticketInfoJson = await ticketInfoResponse.Content.ReadAsStringAsync();
                var ticketInfo = JsonSerializer.Deserialize<TicketResponse>(ticketInfoJson);
                var flightNumber = ticketInfo?.FlightNumber;

                var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/tickets/{ticketUid}");
                if (!string.IsNullOrEmpty(authHeader))
                    request.Headers.Add("Authorization", authHeader);

                var response = await _ticketsClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    ReturnBalanceHistoryRequest body = new ReturnBalanceHistoryRequest() { TicketUid = ticketUid };
                    var bonusRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/privilege/return-balance")
                    {
                        Content = JsonContent.Create(body)
                    };
                    if (!string.IsNullOrEmpty(authHeader))
                        bonusRequest.Headers.Add("Authorization", authHeader);

                    try
                    {
                        await _privilegeClient.SendAsync(bonusRequest);
                        _queueService.Enqueue(new RetryRequest()
                        {
                            Client = _privilegeClient,
                            RequestBody = bonusRequest,
                            Attempts = 0,
                            CreatedAt = DateTime.Now,
                            Username = username,
                            Api = "/api/v1/privilege/return-balance",
                            HttpMethod = HttpMethod.Post,
                            Body = body,
                            AccessToken = token
                        });
                    }
                    catch (Exception _)
                    {
                        _queueService.Enqueue(new RetryRequest()
                        {
                            Client = _privilegeClient,
                            RequestBody = bonusRequest,
                            Attempts = 0,
                            CreatedAt = DateTime.Now,
                            Username = username,
                            Api = "/api/v1/privilege/return-balance",
                            HttpMethod = HttpMethod.Post,
                            Body = body,
                            AccessToken = token
                        });
                    }

                    try
                    {
                        var releaseRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/flights/{flightNumber}/release");
                        if (!string.IsNullOrEmpty(authHeader))
                            releaseRequest.Headers.Add("Authorization", authHeader);
                        await _flightsClient.SendAsync(releaseRequest);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to release seat for flight {FlightNumber}", flightNumber);
                    }

                    var message = new Message<Null, string>
                    {
                        Value = JsonSerializer.Serialize(new
                        {
                            EventType = "TicketReturned",
                            Timestamp = DateTime.UtcNow,
                            Username = username,
                            Payload = JsonSerializer.Serialize(new { ticketUid })
                        })
                    };
                    var deliveryResult = await _producer.ProduceAsync("rsoi-events", message, CancellationToken.None)
                        .WaitAsync(TimeSpan.FromSeconds(5));
                }

                return NoContent();
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Kafka timeout, but ticket was purchased");
                return NoContent();
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "Failed to produce Kafka message");
                return NoContent();
            }
            catch (Exception ex)
            {
                if (ex is ServerDiedException)
                    return StatusCode(503, new ErrorResponse { Message = ex.Message });

                if (ex is UnauthorizedAccessException)
                    return StatusCode(401, new ErrorResponse { Message = ex.Message });

                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpGet("me")]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                GetUsernameFromToken();
                
                var ticketsRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tickets");
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                    ticketsRequest.Headers.Add("Authorization", authHeader);
                
                List<TicketResponse>? ticketsResponse = await _circuitBreakersController.ExecuteSoftAsync(
                    Services.Ticket,
                    async () => await SendRequest<List<TicketResponse>>(_ticketsClient, ticketsRequest),
                    () => _fallbacks.GetAllTicketsFallback()
                );
                
                var bonusRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/privilege");
                if (!string.IsNullOrEmpty(authHeader))
                    bonusRequest.Headers.Add("Authorization", authHeader);
                
                PrivilegeShortInfo? bonusResponse = await _circuitBreakersController.ExecuteSoftAsync(
                    Services.Bonus,
                    async () => await SendRequest<PrivilegeShortInfo>(_privilegeClient, bonusRequest),
                    () => _fallbacks.GetPrivilegeShortInfoFallback()
                );
                    
                var userInfo = new UserInfoResponse
                {
                    Tickets = ticketsResponse ?? new List<TicketResponse>(),
                    Privilege = bonusResponse ?? new PrivilegeShortInfo(),
                };
                    
                object response;
                if (bonusResponse?.Status == "ERROR")
                {
                    response = new
                    {
                        tickets = ticketsResponse ?? new List<TicketResponse>(),
                        privilege = new { }
                    };
                }
                else
                {
                    response = new UserInfoResponse
                    {
                        Tickets = ticketsResponse ?? new List<TicketResponse>(),
                        Privilege = bonusResponse
                    };
                }
            
                return Ok(response);
            }
            catch (Exception ex)
            {
                if (ex is ServerDiedException)
                    return StatusCode(503, new ErrorResponse { Message = ex.Message });
                
                if (ex is UnauthorizedAccessException)
                    return StatusCode(401, new ErrorResponse { Message = ex.Message });
                
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpGet("privilege")]
        public async Task<IActionResult> GetPrivilegeInfo()
        {
            try
            {
                GetUsernameFromToken();
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/privilege");
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    request.Headers.Add("Authorization", authHeader);
                }
                
                PrivilegeInfoResponse? response = await _circuitBreakersController.ExecuteAsync(
                    Services.Bonus,
                    async () => await SendRequest<PrivilegeInfoResponse>(_privilegeClient, request)
                );
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                if (ex is ServerDiedException)
                    return StatusCode(503, new ErrorResponse { Message = ex.Message });
                
                if (ex is UnauthorizedAccessException)
                    return StatusCode(401, new ErrorResponse { Message = ex.Message });
                
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        [HttpPost("privilege/update-balance")]
        public async Task<IActionResult> UpdatePrivilegeInfo([FromBody] UpdateBalanceHistoryRequest historyRequest)
        {
            PrivilegeInfoResponse privilegeInfo = null;
            try
            {
                GetUsernameFromToken();
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/privilege/update-balance")
                {
                    Content = JsonContent.Create(historyRequest)
                };
                
                if (!string.IsNullOrEmpty(authHeader))
                    request.Headers.Add("Authorization", authHeader);
                
                var response = await _privilegeClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                
                var content = await response.Content.ReadAsStringAsync();
                privilegeInfo = JsonSerializer.Deserialize<PrivilegeInfoResponse>(content);
                
                var message = new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(new
                    {
                        EventType = "BonusBalanceWasUpdated",
                        Timestamp = DateTime.UtcNow,
                        Username =  GetUsernameFromToken(),
                        Payload = JsonSerializer.Serialize(new { historyRequest.TicketUid, historyRequest.BalanceDiff, historyRequest.OperationType })
                    })
                };
                var deliveryResult = await _producer.ProduceAsync("rsoi-events", message, CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(5));
                
                return Ok(privilegeInfo);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Kafka timeout, but ticket was purchased");
                return Ok(privilegeInfo);
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "Failed to produce Kafka message");
                return Ok(privilegeInfo);
            }
            catch (Exception ex)
            {
                if (ex is ServerDiedException)
                    return StatusCode(503, new ErrorResponse { Message = ex.Message });
                
                if (ex is UnauthorizedAccessException)
                    return StatusCode(401, new ErrorResponse { Message = ex.Message });
                
                return StatusCode(500, new ErrorResponse { Message = ex.Message });
            }
        }
        
        private async Task<T?> SendRequest<T>(HttpClient client, HttpRequestMessage requestMessage)
        {
            HttpResponseMessage response = await client.SendAsync(requestMessage);
                
            // ошибку обработает щиток и я не достану текст, так что нет особой разницы, что кидать
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(body + $"{response.StatusCode}", null, response.StatusCode);
            }
                
            string content = await response.Content.ReadAsStringAsync();
            T? responseModel = JsonSerializer.Deserialize<T>(content);
            
            return responseModel;
        }
        
        private async Task<(int paidByBonuses, int paidByMoney)> CalculatePayment(int ticketPrice, bool paidFromBalance, string username)
        {
            if (!paidFromBalance)
                return (0, ticketPrice);
            
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/privilege");
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
                request.Headers.Add("Authorization", authHeader);
            
            PrivilegeInfoResponse? response = await _circuitBreakersController.ExecuteSoftAsync(
                Services.Flight,
                async () => await SendRequest<PrivilegeInfoResponse>(_privilegeClient, request),
                () => _fallbacks.GetPrivilegeInfoResponseFallback()
            );

            if (response != null)
            {
                var availableBonuses = response.Balance;
                var bonusesToUse = Math.Min(availableBonuses, ticketPrice);
                
                return (bonusesToUse, ticketPrice - bonusesToUse);
            }
            
            return (0, ticketPrice);
        }
        
        private async Task<PrivilegeShortInfo> UpdateBonusBalance(string username, Guid ticketUid, int paidByBonuses, int ticketPrice)
        {
            bool isPaidByBonuses = paidByBonuses != 0;

            var bonusAmount = (int)(ticketPrice * 0.1);
            
            var debitRequest = new UpdateBalanceHistoryRequest
            {
                TicketUid = ticketUid,
                BalanceDiff = isPaidByBonuses ? -paidByBonuses : bonusAmount,
                OperationType = isPaidByBonuses ? "DEBIT_THE_ACCOUNT" : "FILL_IN_BALANCE"
            };
            
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/privilege/update-balance")
            {
                Content = JsonContent.Create(debitRequest)
            };
            
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
                httpRequest.Headers.Add("Authorization", authHeader);

            try
            {
                await _privilegeClient.SendAsync(httpRequest);
            }
            catch (Exception _)
            {
                throw new ServerDiedException("Bonus Service unavailable");
            }
            
            var privilegeRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/privilege");
            if (!string.IsNullOrEmpty(authHeader))
                privilegeRequest.Headers.Add("Authorization", authHeader);
            
            PrivilegeInfoResponse? response = await _circuitBreakersController.ExecuteAsync(
                Services.Bonus,
                async () => await SendRequest<PrivilegeInfoResponse>(_privilegeClient, privilegeRequest)
            );
            
            return new PrivilegeShortInfo
            {
                Balance = response?.Balance ?? 0,
                Status = response?.Status ?? "BRONZE"
            };
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