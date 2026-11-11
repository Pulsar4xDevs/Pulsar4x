using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.Modding
{
    /// <summary>
    /// One reference in start or load data that points at an id which is not in the loaded data.
    /// The fields are what a mod author needs: the mod, the data file, the object that holds the
    /// reference, the field on it, and the id that could not be found.
    /// </summary>
    public class ModLoadError
    {
        /// <summary>Mod namespace the object was loaded from, e.g. "Pulsar4x".</summary>
        public string ModNamespace { get; }

        /// <summary>The manifest-relative data file, e.g. "ScenarioFiles/systems/sol/factions.json".</summary>
        public string FileName { get; }

        /// <summary>The blueprint or design kind, e.g. "FactionBlueprint", "ColonyBlueprint".</summary>
        public string ObjectType { get; }

        /// <summary>The UniqueID of the object that holds the bad reference.</summary>
        public string ObjectId { get; }

        /// <summary>Name of the field that holds the bad reference, e.g. "ComponentDesigns".</summary>
        public string Field { get; }

        /// <summary>The id nothing was found for.</summary>
        public string MissingId { get; }
        public ModLoadError(
            string modNamespace,
            string fileName,
            string objectType,
            string objectId,
            string field,
            string missingId)
        {
            ModNamespace = modNamespace ?? "";
            FileName = fileName ?? "";
            ObjectType = objectType ?? "";
            ObjectId = objectId ?? "";
            Field = field ?? "";
            MissingId = missingId ?? "";
        }

        /// <summary>
        /// The whole thing in one sentence, naming every part. Useful when there is no blueprint in
        /// hand to take the mod and file from.
        /// </summary>
        public static string Format(
            string modNamespace,
            string fileName,
            string objectType,
            string objectId,
            string field,
            string missingId)
        {
            return $"Mod '{NoValue(modNamespace)}', file '{NoValue(fileName)}': {NoValue(objectType)} "
                + $"'{NoValue(objectId)}' field '{NoValue(field)}' refers to '{NoValue(missingId)}', "
                + "which is not in the loaded data.";
        }

        public override string ToString() => Format(ModNamespace, FileName, ObjectType, ObjectId, Field, MissingId);

        private static string NoValue(string value) => string.IsNullOrEmpty(value) ? "(unknown)" : value;
    }

    /// <summary>
    /// Thrown when start or mod data refers to an id that is not in the loaded data. Carries every
    /// reference found in the failing pass, so a player sees all the bad ids at once rather than
    /// the first one only. Also carries a short message ready for the new-game error banner.
    /// </summary>
    public class ModLoadException : Exception
    {
        public IReadOnlyList<ModLoadError> Errors { get; }

        /// <summary>The multi-line text shown to the player: a short lead-in plus every error.</summary>
        public string DisplayMessage => BuildDisplayMessage(Errors);

        public ModLoadException(IReadOnlyList<ModLoadError> errors, Exception? innerException = null)
            : base(BuildDisplayMessage(errors), innerException)
        {
            Errors = errors;
        }

        public ModLoadException(ModLoadError error, Exception? innerException = null)
            : this(new[] { error }, innerException)
        {
        }

        private static string BuildDisplayMessage(IReadOnlyList<ModLoadError> errors)
        {
            var text = new StringBuilder();
            text.Append(errors.Count == 1
                ? "The game data refers to an id that was not found:"
                : $"The game data refers to {errors.Count} ids that were not found:");
            foreach (var error in errors)
            {
                text.Append('\n');
                text.Append("  \u2022 ");
                text.Append(error);
            }
            return text.ToString();
        }
    }
}
