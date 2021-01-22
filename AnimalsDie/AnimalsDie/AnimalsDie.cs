namespace AnimalsDie
{
    using StardewModdingAPI;
    using StardewValley;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

    public class AnimalsDie : Mod
    {
        private List<string> messages = new List<string>();

        private List<FarmAnimal> lastDaysAnimals = null;

        /// <summary>
        /// The current config file
        /// </summary>
        private AnimalsDieConfig config;

        public override void Entry(IModHelper helper)
        {
            config = Helper.ReadConfig<AnimalsDieConfig>();
            AnimalsDieConfig.VerifyConfigValues(config, this);

            Helper.Events.GameLoop.SaveLoaded += delegate { lastDaysAnimals = null; messages = new List<string>(); };
            Helper.Events.GameLoop.DayStarted += delegate { CheckForAnimalDeath(); };
            Helper.Events.GameLoop.UpdateTicked += delegate { TryToSendMessage(); };
            Helper.Events.GameLoop.DayEnding += delegate { SaveFarmAnimalsFromPriorDay(); };

            Helper.Events.GameLoop.GameLaunched += delegate { AnimalsDieConfig.SetUpModConfigMenu(config, this); };
        }

        public void DebugLog(object o)
        {
            Monitor.Log(o == null ? "null" : o.ToString(), LogLevel.Debug);
        }

        private void PostMessage(object o)
        {
            messages.Add(o == null ? "null" : o.ToString());
        }

        /// <summary>
        /// For some reason Game1.getFarm().animals is always empty for me?
        /// </summary>
        /// <returns></returns>
        private List<FarmAnimal> GetFarmAnimals()
        {
            return Game1.getFarm().getAllFarmAnimals();
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

        private void CheckForAnimalDeath()
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            CheckForWildAnimalAttack();

            foreach (var animal in GetFarmAnimals())
            {
                int starvation = CalculateStarvation(animal);

                if (config.DeathByStarvation && starvation > config.DaysToDieDueToStarvation)
                {
                    KillAnimal(animal, "starvation");
                    continue;
                }

                int illness = CalculateIllness(animal);

                if (config.DeathByIllness && illness > config.IllnessScoreToDie)
                {
                    KillAnimal(animal, "illness");
                    continue;
                }

                if (config.DeathByOldAge && ShouldDieOfOldAge(animal))
                {
                    KillAnimal(animal, "old age");
                    continue;
                }
            }
        }

        private void SaveFarmAnimalsFromPriorDay()
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            // shallow copy is enough
            lastDaysAnimals = new List<FarmAnimal>(GetFarmAnimals());
        }

        private void CheckForWildAnimalAttack()
        {
            if (lastDaysAnimals == null)
            {
                return;
            }

            var removedAnimals = lastDaysAnimals.Except(GetFarmAnimals());

            // if the last animal died we simply assume it was a wild animal attack
            bool wasAnimalAttack = GetFarmAnimals().Count == 0;

            foreach (var animal in GetFarmAnimals())
            {
                if (animal.moodMessage == 5)
                {
                    wasAnimalAttack = true;
                    break;
                }
            }

            if (wasAnimalAttack)
            {
                foreach (var deadAnimal in removedAnimals)
                {
                    if (deadAnimal != null)
                    {
                        CalculateDeathMessage(deadAnimal, "a wild animal attack");
                    }
                }
            }
        }

        private int CalculateIllness(FarmAnimal animal)
        {
            string moddata;
            animal.modData.TryGetValue($"{this.ModManifest.UniqueID}/illness", out moddata);

            int illness = 0;

            if (!string.IsNullOrEmpty(moddata))
            {
                illness = int.Parse(moddata);
            }

            if (animal.moodMessage == 5 || animal.moodMessage == 6 || animal.fullness < 30)
            {
                // was trapped outdoors overnight
                if (animal.moodMessage == 6)
                {
                    illness++;
                }

                // an animal died to a wild animal attack (if it was also outdoors illness increases by 2)
                if (animal.moodMessage == 5)
                {
                    illness++;
                }

                // animal was not fed (also works if it was outside and found nothing to eat)
                if (animal.fullness < 30)
                {
                    illness++;
                }
            }
            else if (illness > 0)
            {
                illness--;
            }

            animal.modData[$"{this.ModManifest.UniqueID}/illness"] = illness.ToString();

            return illness;
        }

        private int CalculateStarvation(FarmAnimal animal)
        {
            string moddata;
            animal.modData.TryGetValue($"{this.ModManifest.UniqueID}/starvation", out moddata);

            int starvation = 0;

            if (!string.IsNullOrEmpty(moddata))
            {
                starvation = int.Parse(moddata);
            }

            if (animal.fullness < 30)
            {
                starvation++;
            }
            else
            {
                starvation = 0;
            }

            animal.modData[$"{this.ModManifest.UniqueID}/starvation"] = starvation.ToString();

            return starvation;
        }

        private bool ShouldDieOfOldAge(FarmAnimal animal)
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

        private double Map(double from, double fromMin, double fromMax, double toMin, double toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;

            return to;
        }

        private Tuple<int, int> GetMinAndMaxAnimalAgeInYears(FarmAnimal animal)
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

        private AnimalType GetAnimalType(FarmAnimal animal)
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

        private void CalculateDeathMessage(FarmAnimal animal, string cause)
        {
            // age is the value that doesn't increase if you haven't fed the animal
            int age = animal.GetDaysOwned() + 1;

            string ageString;

            if (age >= 28 * 4)
            {
                int val = age / (28 * 4) + 1;
                ageString = val == 1 ? "1 year" : val + " years";

                int restAge = age - (val * 28 * 4);
                if (restAge >= 28)
                {
                    val = (restAge / 28) + 1;
                    ageString += val == 1 ? " and 1 month" : $" and {val} months";
                }
            }
            else
            {
                if (age >= 28)
                {
                    int val = (age / 28) + 1;
                    ageString = val == 1 ? "1 month" : val + " months";
                }
                else
                {
                    ageString = age == 1 ? "1 day" : age + " days";
                }
            }

            string happiness;

            if (animal.happiness < 30)
            {
                happiness = "sad";
            }
            else if (animal.happiness < 200)
            {
                happiness = "fine";
            }
            else
            {
                happiness = "happy";
            }

            double hearts = animal.friendshipTowardFarmer < 1000 ? animal.friendshipTowardFarmer / 200.0 : 5;

            double withHalfHearts = (int)(hearts * 2) / 2;
            string loveString = withHalfHearts == 1 ? "1 heart" : withHalfHearts + " hearts";

            PostMessage($"Your {animal.type.ToString().ToLower()} {animal.name} died due to {cause}.\n{animal.name} got {ageString} old, was feeling {happiness} and had {loveString} of friendship towards you.");
        }

        /// <summary>
        /// This is the way the game deletes animals when you sell them
        /// </summary>
        /// <param name="animal"></param>
        /// <param name="cause"></param>
        private void KillAnimal(FarmAnimal animal, string cause)
        {
            (animal.home.indoors.Value as AnimalHouse).animalsThatLiveHere.Remove(animal.myID);
            (animal.home.indoors.Value as AnimalHouse).animals.Remove(animal.myID);
            Game1.getFarm().animals.Remove(animal.myID);

            animal.health.Value = -1;

            if (animal.foundGrass != null && FarmAnimal.reservedGrass.Contains(animal.foundGrass))
            {
                FarmAnimal.reservedGrass.Remove(animal.foundGrass);
            }

            CalculateDeathMessage(animal, cause);
        }
    }
}