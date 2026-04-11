namespace PSA.Gameplay
{
    // Seat Movement
    public struct GridUpdatedEvent
    {
    }

    // Passenger onSeat
    public struct PassengerSeatedEvent
    {
        public Seats.Seat seat;
    }

    // Time Finish
    public struct TimeUpEvent
    {
    }

    // Level Complete
    public struct LevelCompletedEvent
    {
        public bool isVictory;
    }

    // UI Manager triggers
    public struct RestartLevelEvent
    {
    }

    // UI Manager triggers
    public struct NextLevelEvent
    {
    }

    // GameManager Triggers
    public struct LevelStartedEvent
    {
        public Data.LevelData levelData;
    }

    // PassengerManager triggers
    public struct AllPassengersSeatedEvent
    {
    }

    // GridManager triggers when the physical grid is fully instantiated
    public struct GridReadyEvent
    {
    }
}