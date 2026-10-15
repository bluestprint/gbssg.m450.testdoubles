using Moq;

namespace TestDoublesExcercise.Tests;

public sealed class ReservationServiceTests
{
    [Fact]
    public void Reserve_WithEnoughSeats_ReturnsTrue()
    {
        // Arrange: Stub mit 3 freien Plätzen, Repository mit erfolgreichem Speichern,
        //          eigener Sender-Spy und ReservationService.
        var availability = new FixedSeatAvailabilityStub("EVT-1", availableSeats: 3);
        var repository = new RecordingReservationRepository(trySaveResult: true);
        var sender = new RecordingConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act: 2 Plätze reservieren.
        var result = service.Reserve("alice@example.com", "EVT-1", seatCount: 2);

        // Assert: Ergebnis ist true.
        Assert.True(result);
        Assert.Single(repository.SavedReservations);
        Assert.Single(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotSaveOrSend()
    {
        // Arrange: Stub mit 1 freiem Platz und Doubles für Repository und Sender.
        var availability = new FixedSeatAvailabilityStub("EVT-1", availableSeats: 1);
        var repository = new RecordingReservationRepository();
        var sender = new RecordingConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act: 2 Plätze reservieren.
        var result = service.Reserve("alice@example.com", "EVT-1", seatCount: 2);

        // Assert: Ergebnis ist false; weder Speichern noch Senden wurde aufgerufen.
        Assert.False(result);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithExactlyEnoughSeats_Succeeds()
    {
        // Arrange: Stub mit genau 2 freien Plätzen.
        var availability = new FixedSeatAvailabilityStub("EVT-1", availableSeats: 2);
        var repository = new RecordingReservationRepository();
        var sender = new RecordingConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act: 2 Plätze reservieren.
        var result = service.Reserve("alice@example.com", "EVT-1", seatCount: 2);

        // Assert: Ergebnis ist true.
        Assert.True(result);
    }

    [Fact]
    public void Reserve_WithZeroSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Doubles, deren Aufrufe sich prüfen lassen.
        var availability = new FixedSeatAvailabilityStub("EVT-1", availableSeats: 5);
        var repository = new RecordingReservationRepository();
        var sender = new RecordingConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act & Assert: ArgumentOutOfRangeException; keine Abhängigkeit wurde aufgerufen.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.Reserve("alice@example.com", "EVT-1", seatCount: 0));

        Assert.Equal(0, availability.CallCount);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithNegativeSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Doubles, deren Aufrufe sich prüfen lassen.
        var availability = new FixedSeatAvailabilityStub("EVT-1", availableSeats: 5);
        var repository = new RecordingReservationRepository();
        var sender = new RecordingConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act & Assert: ArgumentOutOfRangeException; keine Abhängigkeit wurde aufgerufen.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.Reserve("alice@example.com", "EVT-1", seatCount: -1));

        Assert.Equal(0, availability.CallCount);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithEmptyEventCode_ThrowsBeforeCallingDependencies()
    {
        // Arrange: Doubles, deren Aufrufe sich prüfen lassen.
        var availability = new FixedSeatAvailabilityStub("EVT-1", availableSeats: 5);
        var repository = new RecordingReservationRepository();
        var sender = new RecordingConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act & Assert: ArgumentException; keine Abhängigkeit wurde aufgerufen.
        Assert.Throws<ArgumentException>(
            () => service.Reserve("alice@example.com", "", seatCount: 2));

        Assert.Equal(0, availability.CallCount);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithWhitespaceEmail_ThrowsBeforeCallingDependencies()
    {
        // Aufgabe 5 – Begründung: Hier interessiert uns ausschliesslich, dass KEINE
        // der drei Abhängigkeiten aufgerufen wird. Dafür genügt ein generisches
        // Verify(..., Times.Never()) pro Abhängigkeit; eigene Zähllogik in
        // handgeschriebenen Doubles wäre hier nur Mehraufwand ohne Zusatznutzen.
        // Deshalb verwenden wir für diesen Test konsequent Moq für alle drei Doubles.

        // Arrange
        var availability = new Mock<ISeatAvailability>();
        var repository = new Mock<IReservationRepository>();
        var sender = new Mock<IConfirmationSender>();
        var service = new ReservationService(availability.Object, repository.Object, sender.Object);

        // Act & Assert: ArgumentException; keine Abhängigkeit wurde aufgerufen.
        Assert.Throws<ArgumentException>(
            () => service.Reserve("   ", "EVT-1", seatCount: 2));

        availability.Verify(a => a.GetAvailableSeats(It.IsAny<string>()), Times.Never());
        repository.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        sender.Verify(s => s.Send(It.IsAny<Reservation>()), Times.Never());
    }

    [Fact]
    public void Reserve_WhenSavingFails_DoesNotSendConfirmation()
    {
        // Arrange: Stub mit genügend Plätzen, Repository liefert false, eigener Spy.
        var availability = new FixedSeatAvailabilityStub("EVT-1", availableSeats: 3);
        var repository = new RecordingReservationRepository(trySaveResult: false);
        var sender = new RecordingConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act: Reservierung versuchen.
        var result = service.Reserve("alice@example.com", "EVT-1", seatCount: 2);

        // Assert: Ergebnis ist false und der Spy hat keinen Aufruf aufgezeichnet.
        Assert.False(result);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotCallRepository()
    {
        // Arrange: Stub mit zu wenigen Plätzen und Mock<IReservationRepository>.
        var availability = new FixedSeatAvailabilityStub("EVT-1", availableSeats: 1);
        var repository = new Mock<IReservationRepository>();
        var sender = new RecordingConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);

        // Act: Reservierung versuchen.
        var result = service.Reserve("alice@example.com", "EVT-1", seatCount: 2);

        // Assert: Verify(..., Times.Never()) für TrySave.
        Assert.False(result);
        repository.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithEnoughSeats_SavesExactReservationViaMoq()
    {
        // Arrange: Moq-Repository mit Setup für erfolgreiches Speichern.
        var availability = new FixedSeatAvailabilityStub("EVT-1", availableSeats: 5);
        var repository = new Mock<IReservationRepository>();
        repository.Setup(r => r.TrySave(It.IsAny<Reservation>())).Returns(true);
        var sender = new RecordingConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);

        // Act
        var result = service.Reserve("alice@example.com", "EVT-1", seatCount: 3);

        // Assert: Die richtige Reservierung wurde genau einmal gespeichert.
        Assert.True(result);
        repository.Verify(
            r => r.TrySave(It.Is<Reservation>(reservation =>
                reservation.CustomerEmail == "alice@example.com" &&
                reservation.EventCode == "EVT-1" &&
                reservation.SeatCount == 3)),
            Times.Once());
        Assert.Equal("alice@example.com", Assert.Single(sender.SentReservations).CustomerEmail);
    }

    [Fact(Skip = "Fortgeschrittene: ReserveBatch zuerst implementieren")]
    public void ReserveBatch_WhenCombinedSeatsExceedCapacity_DoesNotSaveOrSend()
    {
        // Arrange: Zwei Reservierungen für dieselbe Veranstaltung, die einzeln passen.
        // Act:     Nach Implementierung des Batch-Verfahrens beide zusammen reservieren.
        // Assert:  Ergebnis ist false; kein Batch-Speichern und keine Bestätigung.
        throw new NotImplementedException();
    }

    private sealed class FixedSeatAvailabilityStub(IReadOnlyDictionary<string, int> availableSeatsByEventCode)
        : ISeatAvailability
    {
        public FixedSeatAvailabilityStub(string eventCode, int availableSeats)
            : this(new Dictionary<string, int> { [eventCode] = availableSeats })
        {
        }

        public int CallCount { get; private set; }

        public int GetAvailableSeats(string eventCode)
        {
            CallCount++;
            return availableSeatsByEventCode.GetValueOrDefault(eventCode, 0);
        }
    }

    private sealed class RecordingReservationRepository(bool trySaveResult = true) : IReservationRepository
    {
        public List<Reservation> SavedReservations { get; } = [];

        public bool TrySave(Reservation reservation)
        {
            SavedReservations.Add(reservation);
            return trySaveResult;
        }
    }

    private sealed class RecordingConfirmationSenderSpy : IConfirmationSender
    {
        private readonly List<Reservation> _sentReservations = [];

        public IReadOnlyList<Reservation> SentReservations => _sentReservations;

        public void Send(Reservation reservation) => _sentReservations.Add(reservation);
    }
}
