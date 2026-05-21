// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("JKGKgvIxZ8OQwvIdubKKcYRIS7D/MCU1RpR4A1I74rTnwq8zbIFHlr7OxVssNglwVhQdBqJDUhb8piD7Q12B4pdE8pfvkfPgexxF9GsCZ9GuHHgIwZrNmawO+KDaL7eMj8kW2eOpUC04RNPGR8JLnPcUiY/Nw+EpyEtFSnrIS0BIyEtLStx6v/RV8pCUgl808KpINxy2Ew4vWTNua2XrUC9+he+xAMIB1ZMcspn5/Ir2ZCnqtJjTlxwu9VsAHp4jOZGzan8X+yADEMrP/i3MZ5UNC62lFN9hW1Wn5/032sOGQ+2Ad8Q6syEpddlplFEwTCKnNLe2n3ns6VMKWQnHdAXECxV6yEtoekdMQ2DMAsy9R0tLS09KSebGAc/aQy7ba0hJS0pL");
        private static int[] order = new int[] { 2,5,11,12,4,12,12,8,10,10,13,12,13,13,14 };
        private static int key = 74;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
