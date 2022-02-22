const int wordLen = 5;
const int clueLen = 6;
//const string fileName = "AllWords.txt";// "wordle.txt"
const string fileName = "wordle.txt";
await using var wordsFile = new FileStream(fileName, FileMode.Open); 
using var wordsReader = new StreamReader(wordsFile);
var words = (await wordsReader.ReadToEndAsync()).Split('\n').Where(w => w.Length == wordLen).Select(w => w.ToUpperInvariant()).Where(w => w != "UNOIL").ToArray();
wordsFile.Close();
// wordleAnswers
//const string fileNameA = "AllWords.txt";// "wordleAnswers.txt";
const string fileNameA = "wordleAnswers.txt";
await using var wordsFileA = new FileStream(fileNameA, FileMode.Open);
using var wordsReaderA = new StreamReader(wordsFileA);
var wordsA = (await wordsReaderA.ReadToEndAsync()).Split('\n').Where(w => w.Length == wordLen).Select(w => w.ToUpperInvariant()).Where(w => w != "UNOIL").ToArray();
Array.Sort(words);
Array.Sort(wordsA);
Console.WriteLine("Total words: " + wordsA.Length);
Console.WriteLine();
void Stats(string w)
{
    var boardChars = new char[wordLen, clueLen + 5];
    var boardState = new byte[wordLen, clueLen + 5];
    Console.Write (w + " ");
    var m = 1;
    for (var i = 0; i < wordLen; i++)
    {
        //var c1 = wordsA.Count(word => word[i] == w[i]);
        //Console.WriteLine($"Position {i} is {w[i]}: {c1}");
        //Console.WriteLine($"       Contains {w[i]}: {wordsA.Count(word => word.Contains(w[i]))}");
        boardChars[i,0] = w[i];
        m *= 3;
    }

    m--; // trivial case all match
    var results = new int[m];
    for (var i = 0; i < m; i++)
    {
        var s = new string[wordsA.Length];
        Array.Copy(wordsA, s, wordsA.Length);
        var b = i;
        for (var j = 0; j < wordLen; j++)
        {
            b = Math.DivRem(b, 3, out var r);
            boardState[j, 0] = (byte)r;
        }
        Filter(boardChars, boardState, ref s);
        results[i] = s.Length;
    }

    results = results.Where(r => r != 0).ToArray();
    m = results.Length;
    Array.Sort(results);
    var sum = results.Sum();
    var top10 = string.Join(",", results.TakeLast(10).Reverse());
    var modeS = results.GroupBy(r => r).Select(g => (g.Key, g.Count())).OrderByDescending(p => p.Item2).ToArray();
    var median = (m & 1) == 0 ? (results[m / 2] + results[m / 2 + 1]) / 2f : results[m / 2];
    var mode = string.Join(",", modeS.TakeWhile(m => m.Item2 == modeS[0].Item2).Select(m=>m.Key));
    var mean = (float)results.Average();
    var variance = results.Select(r => (r - mean) * (r - mean)).Average();
    Console.WriteLine($"Count: {m} Sum: {sum} Min: {results[0]} Max10: {top10} Range: {results[^1] - results[0]} Median: {median:N1} Mode: {mode} Mean: {mean:N1} Variance: {variance:N1} Standard Deviation: {MathF.Sqrt(variance):N1}");
}

//Stats("TARES");
//Stats("AEROS");
//Stats("ALOES");
//Stats("URAEI");
//Stats("AUREI");
//Stats("RAILE");
//Stats("RAISE");
//Stats("SOARE");
//Stats("ROATE");
//Stats("ARISE");
//Stats("SERAI");
//Stats("AESIR");
//Stats("ARIEL");
//Stats("REALO");
//Stats("AROSE");
//Stats("ORATE");
//Stats("IRATE");
//Stats("ALOES");
//Stats("REAIS");
//Stats("AYRIE");
//Stats("REOIL");
//Stats("TOILE");
//Stats("RAINE");
//Stats("AEROS");
//Stats("ORIEL");
//Stats("STOAE");
//Stats("COATE");

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
        case "4":
            Experiment();
            break;
        default:
            Environment.Exit(0);
            break;
    }
}

void Experiment()
{
    foreach (var word in words)
    {
        var boardChars = new char[wordLen, clueLen + 5];
        var boardState = new byte[wordLen, clueLen + 5];
        var available = new List<string>(words).ToArray();
        var solutions = wordsA.ToArray();// new string[available.Length];
        //Array.Copy(available, solutions, available.Length);
        var guesses = new byte[26];
        var turn = 0;
        for (; ; )
        {

            string guess;
            if (turn > 0)
            {
                FilterAndOrderTests2(boardChars, boardState, ref available, ref solutions);
                OrderSolutions(ref solutions);
                guess = solutions.Length > 2 ? available[0] : solutions[0];
            }
            else
                guess = "TARES";

            // get clue
            var score = string.Empty;
            for (var a = 0; a < wordLen; a++)
            {
                var c = guess[a];
                if (c == word[a])
                    score += '2';
                else if (word.Contains(c))
                    score += '1';
                else
                    score += '0';
            }

            // enter clue
            var correct = 0;
            for (var a = 0; a < wordLen; a++)
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

            turn++;
            if (correct != wordLen)
                continue;

            if (turn > clueLen)
                Console.WriteLine(word + " " + turn);

            break;
        }
    }

    Console.ReadKey();
}

void Play(bool useHelper, bool external)
{
    var word = words[new Random().Next(words.Length)];
    var boardChars = new char[wordLen, clueLen];
    var boardState = new byte[wordLen, clueLen];
    var available = words;//
    var solutions = wordsA.ToArray();// new string[available.Length];
    //Array.Copy(available, solutions, available.Length);
    var guesses = new byte[26];
    var turn = 0;

    //FilterAndOrderTests(boardChars, boardState, ref available, ref solutions);
    //available = available.Take(2000).ToArray();
    //FilterAndOrderTests2(boardChars, boardState, ref available, ref solutions);
    for (; ; )
    {
        DrawBoard(boardChars, boardState, guesses);
        // show suggestions
        if ((useHelper || external) && turn > 0) // tares cares dares tales pares rates bares nares lores
        {
            if (solutions.Length > 2)
            {
                Console.Write("Try these: ");
                for (var x = 0; x < 9 && x < available.Length; x++)
                    Console.Write(available[x] + " ");
                if (available.Length > 8)
                {
                    Console.WriteLine();
                    Console.WriteLine(available.Length);
                }
            }

            OrderSolutions(ref solutions);
            Console.Write("Possible solutions: ");
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
            while (score.Length != wordLen)
                score = Console.ReadLine() ?? string.Empty;
        }
        else
        {
            for (var a = 0; a < wordLen; a++)
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
        for (var a = 0; a < wordLen; a++)
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

        if (turn++ != clueLen - 1 && correct != wordLen)
        {
            if (useHelper)
                FilterAndOrderTests2(boardChars, boardState, ref available, ref solutions);
            continue;
        }

        DrawBoard(boardChars, boardState, guesses);
        if (correct != wordLen && !external) // you lose
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
    void PrintLine(string a, string b, string c)
    {
        Console.Write(a);
        for (var x = 0; x < wordLen - 1; x++)
            Console.Write(b);
        Console.WriteLine(c);
    }
    Console.Clear();
    PrintLine("┌", "───┬", "───┐");
    for (var x = 0; x < clueLen; x++)
    {
        Console.Write("│ ");
        for (var y = 0; y < wordLen; y++)
        {
            DrawChar(boardChars[y, x], boardState[y, x]);
            Console.Write(" │ ");
        }

        Console.WriteLine();
        if (x < clueLen - 1)
            PrintLine("├", "───┼", "───┤");
        else
            PrintLine("└", "───┴", "───┘");
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

static void Filter(char[,] chars, byte[,] states, ref string[] solutionWords)
{
    var possibleUnknownLetters = new int[26];
    var fixedLetters = new char[wordLen]; // we know these letters so they don't count for possibles
    for (var clueIndex = 0; clueIndex < 11; clueIndex++) // each clue
    {
        if (chars[0, clueIndex] == 0) // no clues left
            break;
        for (var letterIndex = 0; letterIndex < wordLen; letterIndex++) // each letter
        {
            var c = chars[letterIndex, clueIndex];
            if (states[letterIndex, clueIndex] == 2)
                fixedLetters[letterIndex] = c;
        }
    }

    // filter solutions
    List<string> list = new();
    foreach (var word in solutionWords) // for each old word
    {
        var isOk = true;
        for (var clueIndex = 0; clueIndex < clueLen + 5 && isOk; clueIndex++) // each clue
        {
            if (chars[0, clueIndex] == 0) // no clues left
                break;
            for (var letterIndex = 0; letterIndex < wordLen && isOk; letterIndex++) // each letter
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
        for (var letterIndex = 0; letterIndex < wordLen; letterIndex++)
            if (fixedLetters[letterIndex] == 0)
            {
                var c = word[letterIndex];
                if (!fixedLetters.Contains(c))
                    possibleUnknownLetters[c - 'A']++;
            }
    }

    solutionWords = list.ToArray();
}

static void FilterAndOrderTests2(char[,] chars, byte[,] states, ref string[] testWords, ref string[] solutionWords)
{
    var possibleUnknownLetters = new int[26];
    var fixedLetters = new char[wordLen]; // we know these letters so they don't count for possibles
    for (var clueIndex = 0; clueIndex < 11; clueIndex++) // each clue
    {
        if (chars[0, clueIndex] == 0) // no clues left
            break;
        for (var letterIndex = 0; letterIndex < wordLen; letterIndex++) // each letter
        {
            var c = chars[letterIndex, clueIndex];
            if (states[letterIndex, clueIndex] == 2)
                fixedLetters[letterIndex] = c;
        }
    }

    // filter solutions
    List<string> list = new();
    foreach (var word in solutionWords) // for each old word
    {
        var isOk = true;
        for (var clueIndex = 0; clueIndex < clueLen + 5 && isOk; clueIndex++) // each clue
        {
            if (chars[0, clueIndex] == 0) // no clues left
                break;
            for (var letterIndex = 0; letterIndex < wordLen && isOk; letterIndex++) // each letter
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
        for (var letterIndex = 0; letterIndex < wordLen; letterIndex++)
            if (fixedLetters[letterIndex] == 0)
            {
                var c = word[letterIndex];
                if (!fixedLetters.Contains(c))
                    possibleUnknownLetters[c - 'A']++;
            }
    }

    solutionWords = list.ToArray();
    var s = solutionWords;
    // filter test words
    // TODO: we can do much better than this - filter words that provide less information
    list.Clear();
    list.AddRange(testWords.Where(testWord => testWord.Any(t => possibleUnknownLetters[t - 'A'] > 0)));
    testWords = list.ToArray();

    // score test words, 300 unknown match, 100 unknown position, 3 known match, 1 known position
    SortWords(ref testWords, targetWord =>
    {
        var average = .0;
        var clue = new byte[wordLen];
        foreach (var subWord in s)
        {
            var newInfo = false;
            for (var i = 0; i < wordLen; i++)
            {
                var c = targetWord[i];
                if (c == subWord[i])
                {
                    clue[i] = 2;
                    if (fixedLetters[i] == 0)
                        newInfo = true;
                }
                else if (subWord.Contains(c))
                {
                    clue[i] = 1;
                    if (possibleUnknownLetters[c - 'A'] > 0)
                        newInfo = true;
                }
                else
                    clue[i] = 0;
            }

            if (newInfo)
            {

                var count = s.Count(s1 =>
                {

                    for (var i = 0; i < wordLen; i++)
                    {
                        switch (clue[i])
                        {
                            case 2:
                                if (s1[i] != targetWord[i])
                                    return false;
                                break;
                            case 1:
                                if (!s1.Contains(targetWord[i]))
                                    return false;
                                break;
                        }
                    }

                    return true;
                });

                //var boob = s.Where(s1 =>
                //{

                //    for (var i = 0; i < 5; i++)
                //    {
                //        switch (clue[i])
                //        {
                //            case 2:
                //                if (s1[i] != targetWord[i])
                //                    return false;
                //                break;
                //            case 1:
                //                if (!s1.Contains(targetWord[i]))
                //                    return false;
                //                break;
                //        }
                //    }

                //    return true;
                //}).ToArray();
                average += (double) count / s.Length;
            }
            else
                average++;
        }

        var score = -(int)average;
        return score != 0 ? score : int.MinValue;
    });
}

static void FilterAndOrderTests(char[,] chars, byte[,] states, ref string[] testWords, ref string[] solutionWords)
{
    var possibleLetterPositions = new int[wordLen, 26];
    var possibleUnknownLetters = new int[26];
    var possibleKnownLetters = new int[26];
    var fixedLetters = new char[wordLen]; // we know these letters so they don't count for possibles
    for (var clueIndex = 0; clueIndex < clueLen + 5; clueIndex++) // each clue
    {
        if (chars[0, clueIndex] == 0) // no clues left
            break;
        for (var letterIndex = 0; letterIndex < wordLen; letterIndex++) // each letter
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
        for (var clueIndex = 0; clueIndex < clueLen + 5 && isOk; clueIndex++) // each clue
        {
            if (chars[0, clueIndex] == 0) // no clues left
                break;
            for (var letterIndex = 0; letterIndex < wordLen && isOk; letterIndex++) // each letter
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
        for (var letterIndex = 0; letterIndex < wordLen; letterIndex++)
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
    testWords = list.ToArray();

    // score test words, 300 unknown match, 100 unknown position, 3 known match, 1 known position
    SortWords(ref testWords, targetWord =>
    {
        var score = 0;
        for (var letterIndex = 0; letterIndex < wordLen; letterIndex++)
        {
            var c = targetWord[letterIndex];
            score += possibleLetterPositions[letterIndex, c - 'A'] * 100;
            if (!targetWord[..letterIndex].Contains(c))
                score += possibleUnknownLetters[c - 'A'] * 100 + possibleKnownLetters[c - 'A'];
            if (fixedLetters[letterIndex] == c)
                score += 2;
        }
        return score;
    });
}

static void OrderSolutions(ref string[] words)
{
    var list = words;
    SortWords(ref words, targetWord =>
    {
        var score = 0;
        foreach (var testWord in list)
        {
            if (targetWord == testWord)
                continue;
            for (var letterIndex = 0; letterIndex < wordLen; letterIndex++)
            {
                var letter = targetWord[letterIndex];
                if (letter == testWord[letterIndex]) // match
                    score += 2;
                if (!targetWord[..letterIndex].Contains(letter))
                    score += Math.Min(targetWord.Count(c => c == letter), testWord.Count(c => c == letter)); // count of unique occurrences
            }
        }
        return score;
    });
}

static void SortWords(ref string[] words, Func<string, int> scorer)
{
    var list = words;

    var scores = new (string, int)[words.Length];
    Parallel.For(0, scores.Length, targetWordIndex =>
    //for (var targetWordIndex = 0; targetWordIndex < scores.Length; targetWordIndex++)
    {
        var targetWord = list[targetWordIndex];
        scores[targetWordIndex] = (targetWord, scorer(targetWord));
    });

    Array.Sort(scores, (a, b) => b.Item2.CompareTo(a.Item2));
    words = scores.Select(s => s.Item1).ToArray();
}