namespace AstraSyndicate.AI
{
    public readonly struct WaveStartedEvent
    {
        public readonly int Wave;
        public readonly int ShipCount;

        public WaveStartedEvent(int wave, int shipCount)
        {
            Wave = wave;
            ShipCount = shipCount;
        }
    }
}
