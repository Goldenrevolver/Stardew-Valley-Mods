namespace HorseOverhaul
{
    using System;

    public class StateMessage
    {
        public const string GotWaterType = "gotWater";
        public const string GotFoodType = "gotFed";
        public const string GotPettedType = "wasPet";
        public const string GotHeaterType = "gotWater";
        public const string SyncType = "sync";
        public const string SyncRequestType = "syncRequest";

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
            HasHeater = wrapper.HasHeater;
        }

        public Guid HorseID { get; set; }

        public int? StableID { get; set; }

        public bool GotWater { get; set; }

        public bool GotFed { get; set; }

        public bool WasPet { get; set; }

        public int Friendship { get; set; }

        public bool HasHeater { get; set; }
    }
}