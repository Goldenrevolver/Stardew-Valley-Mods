namespace HorseOverhaul
{
    using System;

    public class StateMessage
    {
        public StateMessage()
        {
        }

        public StateMessage(HorseWrapper wrapper)
        {
            HorseID = wrapper.Stable.HorseId;
            GotWater = wrapper.GotWater;
            GotFed = wrapper.GotFed;
            WasPet = wrapper.WasPet;
            Friendship = wrapper.Friendship;
            StableID = wrapper.StableID;
        }

        public Guid HorseID { get; set; }

        public int? StableID { get; set; }

        public bool GotWater { get; set; }

        public bool GotFed { get; set; }

        public bool WasPet { get; set; }

        public int Friendship { get; set; }
    }
}