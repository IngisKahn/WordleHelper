await using var wordsFile = new FileStream("words.txt", FileMode.Open); // assume 5 letter words…
using var wordsReader = new StreamReader(wordsFile);
var words = (await wordsReader.ReadToEndAsync()).Split('\n').Select(w => w.ToUpperInvariant()).ToArray();

for (; ; )
{
    Console.Clear();
    Console.WriteLine("Choose Mode:");
    Console.WriteLine("1: Play");
    Console.WriteLine("2: Play with helper");
    Console.WriteLine("3: Helper");
    switch (Console.ReadLine())
    {
        case "1":
            Play(false, false);
            break;
        case "2":
            Play(true, false);
            break;
        case "3":
            Play(true, true);
            break;
        default:
            Environment.Exit(0);
            break;
    }
}

void Play(bool useHelper, bool external)
{
    var word = words[new Random().Next(words.Length)];
    var boardChars = new char[5, 6];
    var boardState = new byte[5, 6];
    var available = words;//FilterAndOrder(boardChars, boardState, words);
    var solutions = new string[available.Length];
    Array.Copy(available, solutions, available.Length);
    var guesses = new byte[26];
    var turn = 0;

    for (; ; )
    {
        DrawBoard(boardChars, boardState, guesses);
        // show suggestions
        if ((useHelper || external) && turn > 0) // tares cares dares tales pares rates bares nares lores
        {
            if (solutions.Length > 2)
            {
                for (var x = 0; x < 6 && x < available.Length; x++)
                    Console.Write(available[x] + " ");
                if (available.Length > 5)
                {
                    Console.WriteLine();
                    Console.WriteLine(available.Length);
                }
            }

            OrderSolutions(ref solutions);
            for (var x = 0; x < 6 && x < solutions.Length; x++)
                Console.Write(solutions[x] + " ");
            if (solutions.Length > 5)
            {
                Console.WriteLine();
                Console.WriteLine(solutions.Length);
            }

            Console.WriteLine();
        }

        Console.WriteLine("Enter Guess:");
        var guess = (Console.ReadLine() ?? string.Empty).ToUpperInvariant();
        var index = 0;
        for (; index < words.Length; index++)
            if (words[index] == guess)
                break;

        if (index == words.Length)
        {
            Console.WriteLine("Word not found");
            Console.ReadKey();
            continue;
        }

        // get clue
        var score = string.Empty;
        if (external)
        {
            Console.WriteLine("Enter score:");
            while (score.Length != 5)
                score = Console.ReadLine() ?? string.Empty;
        }
        else
        {
            for (var a = 0; a < 5; a++)
            {
                var c = guess[a];
                if (c == word[a])
                    score += '2';
                else if (word.Contains(c))
                    score += '1';
                else
                    score += '0';
            }
        }

        // enter clue
        var correct = 0;
        for (var a = 0; a < 5; a++)
        {
            var c = guess[a];
            boardChars[a, turn] = c;
            switch (score[a])
            {
                case '2':
                    guesses[c - 'A'] = 2;
                    boardState[a, turn] = 2;
                    correct++;
                    break;
                case '1':
                    guesses[c - 'A'] = (byte)Math.Max((int)guesses[c - 'A'], 1);
                    boardState[a, turn] = 1;
                    break;
                default:
                    guesses[c - 'A'] = 255;
                    break;
            }
        }

        if (turn++ != 5 && correct != 5)
        {
            if (useHelper)
                FilterAndOrderTests(boardChars, boardState, ref available, ref solutions);
            continue;
        }

        DrawBoard(boardChars, boardState, guesses);
        if (correct != 5 && !external) // you lose
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(word);
            Console.ForegroundColor = ConsoleColor.White;
        }
        Console.ReadKey();
        break;
    }
}

void DrawBoard(char[,] boardChars, byte[,] boardState, byte[] guesses)
{
    Console.Clear();
    Console.WriteLine("┌───┬───┬───┬───┬───┐");
    for (var x = 0; x < 6; x++)
    {
        Console.Write("│ ");
        for (var y = 0; y < 5; y++)
        {
            DrawChar(boardChars[y, x], boardState[y, x]);
            Console.Write(" │ ");
        }

        Console.WriteLine();
        Console.WriteLine(x < 5 ? "├───┼───┼───┼───┼───┤" : "└───┴───┴───┴───┴───┘");
    }

    for (var x = 0; x < 26; x++)
    {
        Console.Write(' ');
        DrawChar((char)(x + 'A'), guesses[x]);
    }
    Console.WriteLine();

    static void DrawChar(char c, byte state)
    {
        Console.ForegroundColor = state switch
        {
            1 => ConsoleColor.DarkYellow,
            2 => ConsoleColor.DarkGreen,
            255 => ConsoleColor.DarkBlue,
            _ => Console.ForegroundColor
        };
        Console.Write(c == 0 ? ' ' : c);
        Console.ForegroundColor = ConsoleColor.White;
    }
}

static void FilterAndOrderTests(char[,] chars, byte[,] states, ref string[] testWords, ref string[] solutionWords)
{
    var possibleLetterPositions = new int[5, 26];
    var possibleUnknownLetters = new int[26];
    var possibleKnownLetters = new int[26];
    var fixedLetters = new char[5]; // we know these letters so they don't count for possibles
    for (var clueIndex = 0; clueIndex < 5; clueIndex++) // each clue
    {
        if (chars[0, clueIndex] == 0) // no clues left
            break;
        for (var letterIndex = 0; letterIndex < 5; letterIndex++) // each letter
        {
            var c = chars[letterIndex, clueIndex];
            switch (states[letterIndex, clueIndex])
            {
                case 2:
                    fixedLetters[letterIndex] = c;
                    goto case 1;
                case 1:
                    possibleKnownLetters[c - 'A']++;
                    break;
            }
        }
    }

    // filter solutions
    List<string> list = new();
    foreach (var word in solutionWords) // for each old word
    {
        var isOk = true;
        for (var clueIndex = 0; clueIndex < 5 && isOk; clueIndex++) // each clue
        {
            if (chars[0, clueIndex] == 0) // no clues left
                break;
            for (var letterIndex = 0; letterIndex < 5 && isOk; letterIndex++) // each letter
            {
                var c = chars[letterIndex, clueIndex];
                isOk = states[letterIndex, clueIndex] switch
                {
                    0 => !word.Contains(c), // not in word
                    1 => c != word[letterIndex] && word.Contains(c), // somewhere else in word
                    2 => c == word[letterIndex], // matches
                    _ => isOk
                };
            }
        }

        if (!isOk)
            continue;

        list.Add(word);
        for (var letterIndex = 0; letterIndex < 5; letterIndex++)
            if (fixedLetters[letterIndex] == 0)
            {
                var c = word[letterIndex];
                possibleLetterPositions[letterIndex, c - 'A']++;
                if (!fixedLetters.Contains(c))
                    possibleUnknownLetters[c - 'A']++;
            }
    }

    solutionWords = list.ToArray();

    // filter test words
    list.Clear();
    list.AddRange(testWords.Where(testWord => testWord.Any(t => possibleUnknownLetters[t - 'A'] > 0)));

    // score test words, 300 unknown match, 100 unknown position, 3 known match, 1 known position

    var scores = new (string, int)[list.Count];

    Parallel.For(0, scores.Length, targetWordIndex =>
    {
        var score = 0;
        var targetWord = list[targetWordIndex];
        for (var letterIndex = 0; letterIndex < 5; letterIndex++)
        {
            var c = targetWord[letterIndex];
            score += possibleLetterPositions[letterIndex, c - 'A'] * 200;
            if (!targetWord[..letterIndex].Contains(c))
                score += possibleUnknownLetters[c - 'A'] * 100 + possibleKnownLetters[c - 'A'];
            if (fixedLetters[letterIndex] == c)
                score += 2;
        }

        scores[targetWordIndex] = (targetWord, score);
    });

    Array.Sort(scores, (a, b) => b.Item2.CompareTo(a.Item2));
    testWords = scores.Select(s => s.Item1).ToArray();
}

static void OrderSolutions(ref string[] words)
{
    var list = words;
    // order
    var scores = new (string, int)[words.Length];
    Parallel.For(0, scores.Length, targetWordIndex =>
    {
        var score = 0;
        var targetWord = list[targetWordIndex];
        for (var testWordIndex = 0; testWordIndex < scores.Length; testWordIndex++)
        {
            if (targetWordIndex == testWordIndex)
                continue;
            var testWord = list[testWordIndex];
            for (var letterIndex = 0; letterIndex < 5; letterIndex++)
            {
                var letter = targetWord[letterIndex];
                if (letter == testWord[letterIndex]) // match
                    score += 2;
                if (!targetWord[..letterIndex].Contains(letter))
                    score += Math.Min(targetWord.Count(c => c == letter), testWord.Count(c => c == letter)); // count of unique occurrences
            }
        }

        scores[targetWordIndex] = (targetWord, score);
    });

    Array.Sort(scores, (a, b) => b.Item2.CompareTo(a.Item2));
    words = scores.Select(s => s.Item1).ToArray();
}