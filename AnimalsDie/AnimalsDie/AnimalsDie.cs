namespace AnimalsDie
{
    using Harmony;
    using Netcode;
    using StardewModdingAPI;
    using StardewValley;
    using StardewValley.Buildings;
    using StardewValley.Events;
    using System;
    using System.Collections.Generic;

    public enum AnimalType
    {
        Sheep,
        Cow,
        Goat,
        Pig,
        Ostrich,
        Chicken,
        Duck,
        Rabbit,
        Dinosaur,
        Other
    }

    public interface IAnimalsNeedWaterAPI
    {
        List<string> GetCoopsWithWateredTrough();

        List<string> GetBarnsWithWateredTrough();

        bool IsAnimalFull(string displayName);
    }

    //// planned features: healing item, display messages

    public class AnimalsDie : Mod
    {
        private static readonly List<string> messages = new List<string>();

        private static readonly List<Tuple<FarmAnimal, string>> animalsToKill = new List<Tuple<FarmAnimal, string>>();

        private static readonly List<FarmAnimal> sickAnimals = new List<FarmAnimal>();

        private static FarmAnimal wildAnimalVictim;

        private static IAnimalsNeedWaterAPI waterMod;

        /// <summary>
        /// The current config file
        /// </summary>
        private static AnimalsDieConfig config;

        private static AnimalsDie mod;

        public override void Entry(IModHelper helper)
        {
            mod = this;
            config = Helper.ReadConfig<AnimalsDieConfig>();

            AnimalsDieConfig.VerifyConfigValues(config, this);

            Helper.Events.GameLoop.GameLaunched += delegate { SetupWaterMod(); AnimalsDieConfig.SetUpModConfigMenu(config, this, waterMod != null); };

            Helper.Events.GameLoop.DayStarted += delegate { OnDayStarted(); };

            Helper.Events.GameLoop.SaveLoaded += delegate { ResetVariables(); };
            Helper.Events.GameLoop.ReturnedToTitle += delegate { ResetVariables(); };
            Helper.Events.GameLoop.UpdateTicked += delegate { TryToSendMessage(); };

            var harmony = HarmonyInstance.Create(ModManifest.UniqueID);

            try
            {
                harmony.Patch(
                   original: AccessTools.Method(typeof(FarmAnimal), "dayUpdate"),
                   prefix: new HarmonyMethod(typeof(AnimalsDie), nameof(PatchDayUpdate))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(QuestionEvent), "setUp"),
                   postfix: new HarmonyMethod(typeof(AnimalsDie), nameof(DetectPregnancy))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(SoundInTheNightEvent), "makeChangesToLocation"),
                   prefix: new HarmonyMethod(typeof(AnimalsDie), nameof(DetectAnimalAttack))
                );
            }
            catch (Exception e)
            {
                ErrorLog("Error while trying to setup required patches:", e);
            }
        }

        public static bool DetectAnimalAttack(ref SoundInTheNightEvent __instance)
        {
            try
            {
                if (!Game1.IsMasterGame)
                {
                    return true;
                }

                var behavior = mod.Helper.Reflection.GetField<NetInt>(__instance, "behavior");
                var targetBuilding = mod.Helper.Reflection.GetField<Building>(__instance, "targetBuilding");

                if (behavior.GetValue() != null && behavior.GetValue().Value == SoundInTheNightEvent.dogs && targetBuilding != null && targetBuilding.GetValue() != null)
                {
                    AnimalHouse indoors = targetBuilding.GetValue().indoors.Value as AnimalHouse;
                    long idOfRemove = 0L;
                    foreach (long a in indoors.animalsThatLiveHere)
                    {
                        if (!indoors.animals.ContainsKey(a))
                        {
                            idOfRemove = a;
                            break;
                        }
                    }

                    if (!Game1.getFarm().animals.ContainsKey(idOfRemove))
                    {
                        return true;
                    }

                    wildAnimalVictim = Game1.getFarm().animals[idOfRemove];
                }

                return true;
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
        }

        public static void DetectPregnancy(ref QuestionEvent __instance, ref bool __result)
        {
            try
            {
                var whichQuestion = mod.Helper.Reflection.GetField<int>(__instance, "whichQuestion");

                if (!__result && whichQuestion.GetValue() == 2 && __instance.animal != null)
                {
                    string moddata;
                    __instance.animal.modData.TryGetValue($"{mod.ModManifest.UniqueID}/illness", out moddata);

                    // add one point of illness due to pregnancy stress
                    int illness = 1;

                    if (!string.IsNullOrEmpty(moddata))
                    {
                        illness += int.Parse(moddata);
                    }

                    mod.VerboseLog($"{__instance.animal.name} received illness point due to pregnancy");

                    __instance.animal.modData[$"{mod.ModManifest.UniqueID}/illness"] = illness.ToString();
                }
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
            }
        }

        // yes, there is a typo in environtment in the base game and whenever it gets fixed this doesn't work anymore
        [HarmonyPriority(Priority.High)]
        public static bool PatchDayUpdate(ref FarmAnimal __instance, ref GameLocation environtment)
        {
            try
            {
                FarmAnimal animal = __instance;
                if (animal.home == null)
                {
                    mod.DebugLog($"{animal.name} has no home anymore! This should have been fixed at the start of the day. Please report this to the mod page.");
                    return true;
                }

                if (environtment == null)
                {
                    mod.DebugLog($"{animal.name} is nowhere? Please report this to the mod page. A game update or another mod probably caused this.");
                    return true;
                }

                bool wasLeftOutLastNight = false;
                if (!(animal.home.indoors.Value as AnimalHouse).animals.ContainsKey(animal.myID) && environtment is Farm)
                {
                    if (!animal.home.animalDoorOpen.Value)
                    {
                        wasLeftOutLastNight = true;
                    }
                }

                byte actualFullness = animal.fullness.Value;

                if (!wasLeftOutLastNight)
                {
                    if (actualFullness < 200 && animal.home.indoors.Value is AnimalHouse)
                    {
                        for (int i = animal.home.indoors.Value.objects.Count() - 1; i >= 0; i--)
                        {
                            if (animal.home.indoors.Value.objects.Pairs.ElementAt(i).Value.Name.Equals("Hay"))
                            {
                                actualFullness = byte.MaxValue;
                            }
                        }
                    }
                }

                int starvation = CalculateStarvation(animal, actualFullness);

                if (config.DeathByStarvation && starvation >= config.DaysToDieDueToStarvation)
                {
                    animalsToKill.Add(new Tuple<FarmAnimal, string>(animal, "starvation"));
                    return true;
                }

                bool gotWater = false;

                if (waterMod != null)
                {
                    if (animal.isCoopDweller())
                    {
                        gotWater = waterMod.IsAnimalFull(animal.displayName) || (!wasLeftOutLastNight && waterMod.GetCoopsWithWateredTrough().Contains(animal.home.nameOfIndoors.ToLower()));
                    }
                    else
                    {
                        gotWater = waterMod.IsAnimalFull(animal.displayName) || (!wasLeftOutLastNight && waterMod.GetBarnsWithWateredTrough().Contains(animal.home.nameOfIndoors.ToLower()));
                    }

                    int dehydration = CalculateDehydration(animal, gotWater);

                    if (config.DeathByDehydrationWithAnimalsNeedWaterMod && dehydration >= config.DaysToDieDueToDehydrationWithAnimalsNeedWaterMod)
                    {
                        animalsToKill.Add(new Tuple<FarmAnimal, string>(animal, "dehydration"));
                        return true;
                    }
                }

                int illness = CalculateIllness(animal, actualFullness, gotWater, wasLeftOutLastNight);

                if (config.DeathByIllness && illness >= config.IllnessScoreToDie)
                {
                    animalsToKill.Add(new Tuple<FarmAnimal, string>(animal, "illness"));
                    return true;
                }
                else if (illness >= config.IllnessScoreToDie / 2)
                {
                    sickAnimals.Add(animal);
                }

                if (config.DeathByOldAge && ShouldDieOfOldAge(animal))
                {
                    animalsToKill.Add(new Tuple<FarmAnimal, string>(animal, "oldAge"));
                    return true;
                }

                return true;
            }
            catch (Exception e)
            {
                mod.ErrorLog("There was an exception in a patch", e);
                return true;
            }
        }

        public void DebugLog(object o)
        {
            Monitor.Log(o == null ? "null" : o.ToString(), LogLevel.Debug);
        }

        public void ErrorLog(object o, Exception e = null)
        {
            string baseMessage = o == null ? "null" : o.ToString();

            string errorMessage = e == null ? string.Empty : $"\n{e.Message}\n{e.StackTrace}";

            Monitor.Log(baseMessage + errorMessage, LogLevel.Error);
        }

        public void VerboseLog(object o)
        {
            if (this.Monitor.IsVerbose)
            {
                Monitor.Log(o == null ? "null" : o.ToString(), LogLevel.Debug);
            }
        }

        private static void PostMessage(object o)
        {
            messages.Add(o == null ? "null" : o.ToString());
        }

        private static bool IsWinter()
        {
            return Game1.currentSeason.Equals("winter");
        }

        private static bool WasColdOutside()
        {
            return Game1.wasRainingYesterday || IsWinter(); ////Game1.isRaining || Game1.isSnowing || Game1.isLightning
        }

        private static bool HasHeater(FarmAnimal animal)
        {
            return animal.home.indoors.Value.numberOfObjectsWithName("Heater") > 0;
        }

        private static int CalculateIllness(FarmAnimal animal, byte actualFullness, bool gotWater, bool wasLeftOutLastNight)
        {
            int addIllness = 0;
            string potentialLog = string.Empty;

            // was trapped outdoors overnight
            if (wasLeftOutLastNight)
            {
                addIllness++;
                potentialLog += "leftOutside ";
                if (WasColdOutside())
                {
                    addIllness++;
                    potentialLog += "alsoCold ";
                }
            }
            else
            {
                // not trapped outside: if it's cold weather
                if (WasColdOutside())
                {
                    // and the door was left open
                    if (animal.home.animalDoorOpen)
                    {
                        // if it's winter it's too cold regardless of if there is a heater (no heater even grants one more point)
                        if (IsWinter())
                        {
                            addIllness++;
                            potentialLog += "openDoorWinter ";
                        }

                        // if it's just cold then a heater is enough to prevent damage
                        if (!HasHeater(animal))
                        {
                            addIllness++;
                            potentialLog += "openDoorNoHeater ";
                        }
                    }
                    else if (IsWinter() && !HasHeater(animal))
                    {
                        addIllness++;
                        potentialLog += "WinterNoHeater ";
                    }
                }
            }

            // an animal died to a wild animal attack
            if (wildAnimalVictim != null)
            {
                addIllness++;
                potentialLog += "wildAnimalAttackHappened ";
            }

            // animal was not fed (also works if it was outside and found nothing to eat)
            // if this fails there will be a water check, so a maximum of one illness point if not fed and no water
            if (actualFullness < 30 && config.DeathByStarvation)
            {
                addIllness++;
                potentialLog += "notFed ";
            }
            else if (waterMod != null && config.DeathByDehydrationWithAnimalsNeedWaterMod && !gotWater)
            {
                addIllness++;
                potentialLog += "noWater ";
            }

            string moddata;
            animal.modData.TryGetValue($"{mod.ModManifest.UniqueID}/illness", out moddata);

            int illness = addIllness;

            if (!string.IsNullOrEmpty(moddata))
            {
                illness += int.Parse(moddata);
            }

            // taking care of your animal always reduces illness
            if (illness > 0 && (addIllness == 0 || animal.wasPet.Value))
            {
                addIllness--;
                illness--;
                potentialLog += "healed";
            }

            if (!string.IsNullOrEmpty(potentialLog))
            {
                mod.VerboseLog($"{animal.name} illness change, total: {illness}, new: {addIllness}, reasons: {potentialLog}");
            }

            animal.modData[$"{mod.ModManifest.UniqueID}/illness"] = illness.ToString();

            return illness;
        }

        private static int CalculateDehydration(FarmAnimal animal, bool gotWater)
        {
            string moddata;
            animal.modData.TryGetValue($"{mod.ModManifest.UniqueID}/dehydration", out moddata);

            int dehydration = 0;

            if (!string.IsNullOrEmpty(moddata))
            {
                dehydration = int.Parse(moddata);
            }

            if (!gotWater)
            {
                dehydration++;
                mod.VerboseLog($"{animal.name} didn't get water, dehydration: {dehydration}");
            }
            else
            {
                dehydration = 0;
            }

            animal.modData[$"{mod.ModManifest.UniqueID}/dehydration"] = dehydration.ToString();

            return dehydration;
        }

        private static int CalculateStarvation(FarmAnimal animal, byte actualFullness)
        {
            string moddata;
            animal.modData.TryGetValue($"{mod.ModManifest.UniqueID}/starvation", out moddata);

            int starvation = 0;

            if (!string.IsNullOrEmpty(moddata))
            {
                starvation = int.Parse(moddata);
            }

            if (actualFullness < 30)
            {
                starvation++;
                mod.VerboseLog($"{animal.name} didn't get fed, fullness: {actualFullness}, starvation: {starvation}");
            }
            else
            {
                starvation = 0;
            }

            animal.modData[$"{mod.ModManifest.UniqueID}/starvation"] = starvation.ToString();

            return starvation;
        }

        private static bool ShouldDieOfOldAge(FarmAnimal animal)
        {
            int age = animal.GetDaysOwned();

            var ages = GetMinAndMaxAnimalAgeInYears(animal);

            // convert years to days
            int minAge = ages.Item1 * 28 * 4;
            int maxAge = ages.Item2 * 28 * 4;

            if (age >= minAge)
            {
                double ran = Game1.random.NextDouble();

                double mappedValue = Map(age, minAge, maxAge, 0, 1);

                if (ran < mappedValue)
                {
                    return true;
                }
            }

            return false;
        }

        private static double Map(double from, double fromMin, double fromMax, double toMin, double toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;

            return to;
        }

        private static Tuple<int, int> GetMinAndMaxAnimalAgeInYears(FarmAnimal animal)
        {
            int barnPlaceHolderAge = 10;
            int coopPlaceHolderAge = 5;

            AnimalType type = GetAnimalType(animal);

            switch (type)
            {
                case AnimalType.Sheep:
                    return new Tuple<int, int>(config.MinAgeSheep, config.MaxAgeSheep);

                case AnimalType.Cow:
                    return new Tuple<int, int>(config.MinAgeCow, config.MaxAgeCow);

                case AnimalType.Goat:
                    return new Tuple<int, int>(config.MinAgeGoat, config.MaxAgeGoat);

                case AnimalType.Pig:
                    return new Tuple<int, int>(config.MinAgeSheep, config.MaxAgeSheep);

                case AnimalType.Ostrich:
                    return new Tuple<int, int>(config.MinAgeOstrich, config.MaxAgeOstrich);

                case AnimalType.Chicken:
                    return new Tuple<int, int>(config.MinAgeChicken, config.MaxAgeChicken);

                case AnimalType.Duck:
                    return new Tuple<int, int>(config.MinAgeDuck, config.MaxAgeDuck);

                case AnimalType.Rabbit:
                    return new Tuple<int, int>(config.MinAgeRabbit, config.MaxAgeRabbit);

                case AnimalType.Dinosaur:
                    return new Tuple<int, int>(config.MinAgeDinosaur, config.MaxAgeDinosaur);

                default:
                    return animal.isCoopDweller() ? new Tuple<int, int>(coopPlaceHolderAge, coopPlaceHolderAge) : new Tuple<int, int>(barnPlaceHolderAge, barnPlaceHolderAge);
            }
        }

        private static AnimalType GetAnimalType(FarmAnimal animal)
        {
            var animalTypes = Enum.GetNames(typeof(AnimalType));

            foreach (var animalType in animalTypes)
            {
                if (animal.type.Contains(animalType))
                {
                    // can't fail because it would not be in Enum.GetNames otherwise
                    return (AnimalType)Enum.Parse(typeof(AnimalType), animalType);
                }
            }

            return AnimalType.Other;
        }

        private static void CalculateDeathMessage(FarmAnimal animal, string cause)
        {
            // animal.age is the value that doesn't increase if you haven't fed the animal
            int age = animal.GetDaysOwned() + 1;
            string causeString = mod.Helper.Translation.Get($"Cause.{cause}");

            string ageString;

            if (age >= 28 * 4)
            {
                int yearCount = (age / (28 * 4)) + 1;
                ageString = mod.Helper.Translation.Get(yearCount == 1 ? "Age.year" : "Age.years", new { yearCount });

                int restAge = age - (yearCount * 28 * 4);
                if (restAge >= 28)
                {
                    int monthCount = (restAge / 28) + 1;

                    if (yearCount == 1)
                    {
                        ageString = mod.Helper.Translation.Get(monthCount == 1 ? "Age.yearAndMonth" : "Age.yearAndMonths", new { yearCount, monthCount });
                    }
                    else
                    {
                        ageString = mod.Helper.Translation.Get(monthCount == 1 ? "Age.yearsAndMonth" : "Age.yearsAndMonths", new { yearCount, monthCount });
                    }
                }
            }
            else
            {
                if (age >= 28)
                {
                    int monthCount = (age / 28) + 1;
                    ageString = mod.Helper.Translation.Get(monthCount == 1 ? "Age.month" : "Age.months", new { monthCount });
                }
                else
                {
                    ageString = mod.Helper.Translation.Get(age == 1 ? "Age.day" : "Age.days", new { dayCount = age });
                }
            }

            string happiness;

            if (animal.happiness < 30)
            {
                happiness = mod.Helper.Translation.Get("Happiness.sad");
            }
            else if (animal.happiness < 200)
            {
                happiness = mod.Helper.Translation.Get("Happiness.fine");
            }
            else
            {
                happiness = mod.Helper.Translation.Get("Happiness.happy");
            }

            double hearts = animal.friendshipTowardFarmer < 1000 ? animal.friendshipTowardFarmer / 200.0 : 5;

            double withHalfHearts = ((int)(hearts * 2.0)) / 2.0;
            string loveString = mod.Helper.Translation.Get(withHalfHearts == 1 ? "Love.heart" : "Love.hearts", new { heartCount = withHalfHearts });

            // if the locale is the default locale, keep the english animal type
            string animalType = string.IsNullOrWhiteSpace(mod.Helper.Translation.Locale) ? animal.type.Value.ToLower() : animal.displayType;

            string message = mod.Helper.Translation.Get("AnimalDeathMessage", new { animalType, animalName = animal.name, cause = causeString, entireAgeText = ageString, happinessText = happiness, lovestring = loveString });

            PostMessage(message);
        }

        /// <summary>
        /// This is the way the game deletes animals when you sell them
        /// </summary>
        /// <param name="animal"></param>
        /// <param name="cause"></param>
        private static void KillAnimal(FarmAnimal animal, string cause)
        {
            mod.VerboseLog($"Killed {animal.name} due to {cause}");

            // right before this Utility.fixAllAnimals gets called, so if it's still not fixed then... it truly doesn't have a home and I don't need to remove it
            if (animal.home != null)
            {
                (animal.home.indoors.Value as AnimalHouse).animalsThatLiveHere.Remove(animal.myID);
                (animal.home.indoors.Value as AnimalHouse).animals.Remove(animal.myID);
            }

            Game1.getFarm().animals.Remove(animal.myID);

            animal.health.Value = -1;

            if (animal.foundGrass != null && FarmAnimal.reservedGrass.Contains(animal.foundGrass))
            {
                FarmAnimal.reservedGrass.Remove(animal.foundGrass);
            }

            CalculateDeathMessage(animal, cause);
        }

        private void ResetVariables()
        {
            wildAnimalVictim = null;
            messages.Clear();
            animalsToKill.Clear();
            sickAnimals.Clear();
        }

        private void SetupWaterMod()
        {
            // the null check is somewhere else
            waterMod = mod.Helper.ModRegistry.GetApi<IAnimalsNeedWaterAPI>("GZhynko.AnimalsNeedWater");
        }

        private void OnDayStarted()
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            CheckHomeStatus();
            KillAnimals();
            DisplayIllMessage();
        }

        private void CheckHomeStatus()
        {
            foreach (var animal in Game1.getFarm().getAllFarmAnimals())
            {
                if (animal.home == null)
                {
                    Utility.fixAllAnimals();
                    DebugLog("Fixed at least one animal from the base game animal home bug");
                    break;
                }
            }
        }

        private void KillAnimals()
        {
            if (wildAnimalVictim != null)
            {
                CalculateDeathMessage(wildAnimalVictim, "wildAnimalAttack");
                wildAnimalVictim = null;
            }

            foreach (var item in animalsToKill)
            {
                if (item.Item1 != null)
                {
                    KillAnimal(item.Item1, item.Item2);
                }
            }

            animalsToKill.Clear();
        }

        private void DisplayIllMessage()
        {
            if (sickAnimals.Count == 1)
            {
                Game1.showGlobalMessage(Helper.Translation.Get("SickAnimalMessage.oneAnimal", new { firstAnimalName = sickAnimals[0].name }));
            }
            else if (sickAnimals.Count == 2)
            {
                Game1.showGlobalMessage(Helper.Translation.Get("SickAnimalMessage.twoAnimals", new { firstAnimalName = sickAnimals[0].name, secondAnimalName = sickAnimals[1].name, }));
            }
            else if (sickAnimals.Count == 3)
            {
                Game1.showGlobalMessage(Helper.Translation.Get("SickAnimalMessage.threeAnimals", new { firstAnimalName = sickAnimals[0].name, secondAnimalName = sickAnimals[1].name, thirdAnimalName = sickAnimals[2].name }));
            }
            else if (sickAnimals.Count == 4)
            {
                Game1.showGlobalMessage(Helper.Translation.Get("SickAnimalMessage.fourAnimals", new { firstAnimalName = sickAnimals[0].name, secondAnimalName = sickAnimals[1].name, thirdAnimalName = sickAnimals[2].name, sickAnimalCount = sickAnimals.Count - 3 }));
            }
            else if (sickAnimals.Count > 4)
            {
                Game1.showGlobalMessage(Helper.Translation.Get("SickAnimalMessage.morethanfourAnimals", new { firstAnimalName = sickAnimals[0].name, secondAnimalName = sickAnimals[1].name, thirdAnimalName = sickAnimals[2].name, sickAnimalCount = sickAnimals.Count - 3 }));
            }

            sickAnimals.Clear();
        }

        private void TryToSendMessage()
        {
            if (!Context.IsWorldReady || !Context.IsMainPlayer)
            {
                return;
            }

            if (messages.Count > 0 && Game1.activeClickableMenu == null)
            {
                Game1.drawObjectDialogue(messages[0]);
                messages.RemoveAt(0);
            }
        }
    }
}