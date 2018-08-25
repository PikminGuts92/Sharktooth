using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth
{
    public class MidiMapping
    {
        private readonly Dictionary<int, int> _mappings;

        private MidiMapping()
        {
            _mappings = new Dictionary<int, int>();
        }

        private int GetMapping(int input) => _mappings.ContainsKey(input)? _mappings[input] : -1;

        public int this[int input] => GetMapping(input);
        
        public static MidiMapping CreateGuitar3()
        {
            const int EXPERT = 59;
            const int HARD   = 41;
            const int MEDIUM = 23;
            const int EASY   =  5;

            const int CH_STARPOWER = 116;
            const int CH_EXPERT    =  94;
            const int CH_HARD      =  82;
            const int CH_MEDIUM    =  70;
            const int CH_EASY      =  58;

            MidiMapping mid = new MidiMapping();

            // Expert guitar
            mid._mappings.Add(EXPERT + 15, CH_STARPOWER ); // SP
            mid._mappings.Add(EXPERT + 10, CH_EXPERT    ); // Open
            mid._mappings.Add(EXPERT +  5, CH_EXPERT + 3); // W3
            mid._mappings.Add(EXPERT +  4, CH_EXPERT + 6); // B3
            mid._mappings.Add(EXPERT +  3, CH_EXPERT + 2); // W2
            mid._mappings.Add(EXPERT +  2, CH_EXPERT + 5); // B2
            mid._mappings.Add(EXPERT +  1, CH_EXPERT + 1); // W1
            mid._mappings.Add(EXPERT     , CH_EXPERT + 4); // B1

            // Hard guitar
            mid._mappings.Add(HARD + 10, CH_HARD    ); // Open
            mid._mappings.Add(HARD +  5, CH_HARD + 3); // W3
            mid._mappings.Add(HARD +  4, CH_HARD + 6); // B3
            mid._mappings.Add(HARD +  3, CH_HARD + 2); // W2
            mid._mappings.Add(HARD +  2, CH_HARD + 5); // B2
            mid._mappings.Add(HARD +  1, CH_HARD + 1); // W1
            mid._mappings.Add(HARD     , CH_HARD + 4); // B1

            // Medium guitar
            mid._mappings.Add(MEDIUM + 10, CH_MEDIUM    ); // Open
            mid._mappings.Add(MEDIUM +  5, CH_MEDIUM + 3); // W3
            mid._mappings.Add(MEDIUM +  4, CH_MEDIUM + 6); // B3
            mid._mappings.Add(MEDIUM +  3, CH_MEDIUM + 2); // W2
            mid._mappings.Add(MEDIUM +  2, CH_MEDIUM + 5); // B2
            mid._mappings.Add(MEDIUM +  1, CH_MEDIUM + 1); // W1
            mid._mappings.Add(MEDIUM     , CH_MEDIUM + 4); // B1

            // Easy guitar
            mid._mappings.Add(EASY + 10, CH_EASY    ); // Open
            mid._mappings.Add(EASY +  5, CH_EASY + 3); // W3
            mid._mappings.Add(EASY +  4, CH_EASY + 6); // B3
            mid._mappings.Add(EASY +  3, CH_EASY + 2); // W2
            mid._mappings.Add(EASY +  2, CH_EASY + 5); // B2
            mid._mappings.Add(EASY +  1, CH_EASY + 1); // W1
            mid._mappings.Add(EASY     , CH_EASY + 4); // B1

            return mid;
        }

        public static MidiMapping CreateGuitar6()
        {
            const int EXPERT = 50;
            const int HARD   = 38;
            const int MEDIUM = 26;
            const int EASY   = 14;

            const int CH_STARPOWER = 116;
            const int CH_EXPERT    =  96;
            const int CH_HARD      =  84;
            const int CH_MEDIUM    =  72;
            const int CH_EASY      =  60;

            MidiMapping mid = new MidiMapping();

            // Expert guitar
            mid._mappings.Add(EXPERT + 5, CH_STARPOWER ); // SP
            mid._mappings.Add(EXPERT + 3, CH_EXPERT + 3);
            mid._mappings.Add(EXPERT + 2, CH_EXPERT + 2);
            mid._mappings.Add(EXPERT + 1, CH_EXPERT + 1);
            mid._mappings.Add(EXPERT    , CH_EXPERT    );

            // Hard guitar
            mid._mappings.Add(HARD + 3, CH_HARD + 3);
            mid._mappings.Add(HARD + 2, CH_HARD + 2);
            mid._mappings.Add(HARD + 1, CH_HARD + 1);
            mid._mappings.Add(HARD    , CH_HARD    );

            // Medium guitar
            mid._mappings.Add(MEDIUM + 3, CH_MEDIUM + 3);
            mid._mappings.Add(MEDIUM + 2, CH_MEDIUM + 2);
            mid._mappings.Add(MEDIUM + 1, CH_MEDIUM + 1);
            mid._mappings.Add(MEDIUM    , CH_MEDIUM    );

            // Easy guitar
            mid._mappings.Add(EASY + 3, CH_EASY + 3);
            mid._mappings.Add(EASY + 2, CH_EASY + 2);
            mid._mappings.Add(EASY + 1, CH_EASY + 1);
            mid._mappings.Add(EASY    , CH_EASY    );

            return mid;
        }

        public static MidiMapping CreateRBDrums()
        {
            const int EXPERT = 50;
            const int HARD = 38; // Hard doesn't seem to be charted...
            const int MEDIUM = 26;
            const int EASY = 14;

            const int CH_STARPOWER = 116;
            const int CH_EXPERT = 96;
            const int CH_HARD = 84;
            const int CH_MEDIUM = 72;
            const int CH_EASY = 60;

            MidiMapping mid = new MidiMapping();

            // Expert drums
            mid._mappings.Add(EXPERT + 5, CH_STARPOWER); // SP
            mid._mappings.Add(EXPERT + 3, CH_EXPERT + 3);
            mid._mappings.Add(EXPERT + 2, CH_EXPERT + 2);
            mid._mappings.Add(EXPERT + 1, CH_EXPERT + 1);
            mid._mappings.Add(EXPERT, CH_EXPERT);

            // Hard drums
            mid._mappings.Add(HARD + 4, CH_HARD + 4);
            mid._mappings.Add(HARD + 3, CH_HARD + 3);
            mid._mappings.Add(HARD + 2, CH_HARD + 2);
            mid._mappings.Add(HARD + 1, CH_HARD + 1);
            mid._mappings.Add(HARD, CH_HARD);

            // Medium drums
            mid._mappings.Add(MEDIUM + 4, CH_MEDIUM + 4);
            mid._mappings.Add(MEDIUM + 3, CH_MEDIUM + 3);
            mid._mappings.Add(MEDIUM + 2, CH_MEDIUM + 2);
            mid._mappings.Add(MEDIUM + 1, CH_MEDIUM + 1);
            mid._mappings.Add(MEDIUM, CH_MEDIUM);

            // Easy drums
            mid._mappings.Add(EASY + 4, CH_EASY + 4);
            mid._mappings.Add(EASY + 3, CH_EASY + 3);
            mid._mappings.Add(EASY + 2, CH_EASY + 2);
            mid._mappings.Add(EASY + 1, CH_EASY + 1);
            mid._mappings.Add(EASY, CH_EASY);

            return mid;
        }
    }
}
