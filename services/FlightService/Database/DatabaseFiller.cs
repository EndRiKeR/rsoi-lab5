using FlightService.Database.Models;
using FlightService.Database.Repositories.Interfaces;

namespace FlightService.Database;

public class DatabaseFiller
{
    private readonly IFlightRepository _flightRepository;
    private readonly IAirportRepository _airportRepository;
    
    public DatabaseFiller(
        IFlightRepository flightRepository,
        IAirportRepository airportRepository)
    {
        _flightRepository = flightRepository;
        _airportRepository = airportRepository;
    }

    public async Task AddTestData()
    {
        try
        {
            if ((await _airportRepository.GetAll()).Count == 0)
                await AddAirport();
            
            if ((await _flightRepository.GetAll()).Count == 0)
                await AddFlight();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    private async Task AddAirport()
    {
        var airports = new List<Airport>
        {
            new Airport()
            {
                Id = 1,
                Name = "Шереметьево",
                City = "Москва",
                Country = "Россия"
            },
            new Airport()
            {
                Id = 2,
                Name = "Пулково",
                City = "Санкт-Петербург",
                Country = "Россия"
            },
        };

        await _airportRepository.AddList(airports);
    }

    private async Task AddFlight()
    {
        var dateTime1 = new DateTime(2026, 05, 23, 20, 0, 0);
        var dateTime2 = new DateTime(2026, 05, 26, 18, 30, 0);
        var dateTime3 = new DateTime(2026, 05, 29, 16, 45, 0);
        
        var flight = new List<Flight> {
            new Flight{
                Id = 1,
                FlightNumber = "AFL031",
                DateTime = DateTime.SpecifyKind(dateTime1, DateTimeKind.Utc),
                Price = 1500,
                FromAirportId = 2,
                ToAirportId = 1,
                AvailableSeats = 160,
            },
            new Flight{
                Id = 2,
                FlightNumber = "AFL259",
                DateTime = DateTime.SpecifyKind(dateTime2, DateTimeKind.Utc),
                Price = 2600,
                FromAirportId = 2,
                ToAirportId = 1,
                AvailableSeats = 100,
            },
            new Flight{
                Id = 3,
                FlightNumber = "AFL946",
                DateTime = DateTime.SpecifyKind(dateTime3, DateTimeKind.Utc),
                Price = 9600,
                FromAirportId = 1,
                ToAirportId = 2,
                AvailableSeats = 120,
            }
        };
        
        await _flightRepository.AddList(flight);
    }
}