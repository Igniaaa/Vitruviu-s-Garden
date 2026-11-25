namespace Basilica.Crafting
{
    public enum ConstructionPieceCategory { Foundations, Floors, Columns, Walls, RoofSupports, Roofs, Stairs }

    public static class ConstructionPieceCategories
    {
        public static ConstructionPieceCategory ForPhase(int phase)
        {
            switch (phase)
            {
                case 1: case 6: return ConstructionPieceCategory.Floors;
                case 2: case 3: return ConstructionPieceCategory.Columns;
                case 4: return ConstructionPieceCategory.Walls;
                case 5: return ConstructionPieceCategory.RoofSupports;
                case 7: case 8: return ConstructionPieceCategory.Roofs;
                case 9: return ConstructionPieceCategory.Stairs;
                default: return ConstructionPieceCategory.Foundations;
            }
        }

        public static string Title(ConstructionPieceCategory category)
        {
            switch (category)
            {
                case ConstructionPieceCategory.Floors: return "Pavimenti";
                case ConstructionPieceCategory.Columns: return "Colonne";
                case ConstructionPieceCategory.Walls: return "Strutture";
                case ConstructionPieceCategory.RoofSupports: return "Supporti tetti";
                case ConstructionPieceCategory.Roofs: return "Tetti";
                case ConstructionPieceCategory.Stairs: return "Scale";
                default: return "Fondamenta";
            }
        }
    }
}
