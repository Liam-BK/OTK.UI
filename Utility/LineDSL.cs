using System.Text;
using OpenTK.Mathematics;

namespace OTK.UI.Utility
{
    /// <summary>
    /// A lightweight expression engine and mini-DSL for evaluating mathematical
    /// and logical expressions at runtime, with support for variables, constants,
    /// lambda-backed accessors, ternary operators, and function calls.
    /// 
    /// <para>
    /// <see cref="LineDSL"/> parses plain-text expressions such as:
    /// <c>"3 + sin(pi)"</c>, <c>"var x = 10"</c>, or
    /// <c>"x = x > 5 ? x * 2 : x / 2"</c>.  
    /// It tokenizes input, converts it to Reverse Polish Notation (RPN),
    /// and evaluates it using an internal execution stack.
    /// </para>
    /// 
    /// <para>
    /// The evaluator maintains three namespaces:
    /// <list type="bullet">
    /// <item><description><b>Constants</b> — predefined values such as <c>pi</c>, <c>e</c>, and boolean flags.</description></item>
    /// <item><description><b>Variables</b> — created with <c>var name = expr</c> and stored internally.</description></item>
    /// <item><description><b>Accessors</b> — externally bound values with custom getter/setter delegates,
    /// allowing expressions to read/write live data via <see cref="AddLambdaRef(string, (Func{double}, Action{double}))"/>.</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// The <see cref="Evaluate(string, bool)"/> method handles full expression semantics,
    /// including assignment rules, constant protection, ternary resolution, and error checking.
    /// For simpler scenarios (no variables, no ternary, no assignment),
    /// <see cref="SimpleEvaluate(string, bool)"/> provides a streamlined evaluation path.
    /// </para>
    /// 
    /// <para>
    /// This class is self-contained and does not depend on external scripting engines.
    /// It is designed for embedding configurable logic inside UI components,
    /// animations, game logic, or user-authored scripts where lightweight 
    /// expression evaluation is desirable.
    /// </para>
    /// </summary>
    public class LineDSL
    {
        /// <summary>
        /// A lookup table of predefined constants available to expressions.
        /// Keys are case-sensitive as written; callers typically lowercase names before lookup.
        /// </summary>
        public Dictionary<string, double> Constants = new()
    {
        {"π", Math.PI},
        {"pi", Math.PI},
        {"e", Math.E},
        {"tau", Math.Tau},
        {"phi", 1.61803398874989484820},
        {"φ", 1.61803398874989484820},
        {"true", 1},
        {"false", 0},
    };

        private Dictionary<string, double> Variables = new();

        private Dictionary<string, (Func<double> getter, Action<double> setter)> Accessors = new();

        /// <summary>
        /// Registers a dynamic reference accessor that behaves like a variable whose
        /// value is retrieved and assigned through external delegates.
        /// </summary>
        /// <param name="name">The identifier for the accessor (case-insensitive).</param>
        /// <param name="dlgt">
        /// A tuple containing a getter and setter delegate:
        /// <list type="bullet">
        /// <item><description><c>getter</c>: retrieves the current value.</description></item>
        /// <item><description><c>setter</c>: writes a new value.</description></item>
        /// </list>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an accessor or variable with the same name already exists,
        /// or if the name conflicts with a constant.
        /// </exception>
        public void AddLambdaRef(string name, (Func<double> getter, Action<double> setter) dlgt)
        {
            name = name.ToLower();
            if (Accessors.ContainsKey(name) || Variables.ContainsKey(name)) throw new InvalidOperationException($"Accessor {name} already exists");
            if (Constants.ContainsKey(name)) throw new InvalidOperationException("Cannot have an Accessor with the same name as a constant.");
            Accessors.Add(name, dlgt);
        }

        /// <summary>
        /// Removes a previously registered lambda-backed accessor.
        /// </summary>
        /// <param name="name">The accessor name (case-insensitive).</param>
        public void RemoveLambdaRef(string name)
        {
            name = name.ToLower();
            Accessors.Remove(name);
        }

        /// <summary>
        /// Removes all registered lambda-based accessors.
        /// </summary>
        public void ClearLambdaRefs()
        {
            Accessors.Clear();
        }

        /// <summary>
        /// Evaluates an expression, optionally supporting assignment semantics.
        /// </summary>
        /// <param name="expression">
        /// The expression string to evaluate.  
        /// Supports forms such as:
        /// <list type="bullet">
        /// <item><description><c>var x = 5 + 2</c> — creates a new variable.</description></item>
        /// <item><description><c>x = x + 1</c> — updates an existing variable or accessor.</description></item>
        /// <item><description><c>3 * sin(pi)</c> — evaluates directly.</description></item>
        /// </list>
        /// </param>
        /// <param name="debug">
        /// Enables debug output during expression processing.
        /// </param>
        /// <returns>
        /// The evaluated numeric value of the expression.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown if variable creation or assignment is invalid,
        /// such as redefining variables, modifying constants,
        /// or assigning to an undefined identifier.
        /// </exception>
        public double Evaluate(string expression, bool debug = false)
        {
            if (expression.Contains('='))
            {
                var split = SplitByAssignment(expression);
                var assignment = split[0].Split(' ');
                if (assignment[0] == "var")
                {
                    if (Variables.ContainsKey(assignment[1])) throw new FormatException($"Variable {assignment[1]} is already defined.");
                    if (Constants.ContainsKey(assignment[1])) throw new FormatException($"Variable {assignment[1]} is already considered a constant.");
                    double result = EvaluateTernary(split[1]);
                    Variables.Add(assignment[1], result);
                    return result;
                }
                else
                {
                    if (Constants.ContainsKey(assignment[0])) throw new FormatException($"Constants are not allowed to be modified");
                    if (!Variables.ContainsKey(assignment[0]) && !Accessors.ContainsKey(assignment[0])) throw new FormatException($"Variable has not been defined so cannot be modified.");
                    var result = EvaluateTernary(split[1]);
                    if (Variables.ContainsKey(assignment[0]))
                        Variables[assignment[0]] = result;
                    else if (Accessors.ContainsKey(assignment[0]))
                    {
                        Accessors[assignment[0]].setter(result);
                    }
                    return result;
                }
            }
            else
            {
                return EvaluateTernary(expression, debug);
            }
        }

        private string[] SplitByAssignment(string expression)
        {
            var array = expression.Split('=');
            if (array.Length > 2)
            {
                throw new FormatException("Cannot have more than one assignment in an expression");
            }
            return array;
        }

        private double EvaluateTernary(string expression, bool debug = false)
        {
            expression = expression.Trim();

            int qIndex = expression.IndexOf('?');
            if (qIndex == -1)
            {
                return SimpleEvaluate(expression);
            }
            int colonIndex = FindNextMatchingTernarySeparator(expression, qIndex);
            if (colonIndex == -1)
            {
                throw new FormatException("Ternary expression missing ':'");
            }
            string condition = expression.Substring(0, qIndex).Trim();
            string trueExpr = expression.Substring(qIndex + 1, colonIndex - qIndex - 1).Trim();
            string falseExpr = expression.Substring(colonIndex + 1).Trim();

            double condValue = SimpleEvaluate(condition);

            if (condValue != 0)
            {
                return EvaluateTernary(trueExpr);
            }
            else
            {
                return EvaluateTernary(falseExpr);
            }
        }

        private static int FindNextMatchingTernarySeparator(string expr, int questionIndex)
        {
            int depth = 0;
            for (int i = questionIndex + 1; i < expr.Length; i++)
            {
                if (expr[i] == '?')
                    depth++;
                else if (expr[i] == ':')
                {
                    if (depth == 0)
                        return i;
                    else
                        depth--;
                }
            }
            return -1;
        }

        /// <summary>
        /// A simplified evaluation pipeline for expressions that do not involve variables,
        /// constants, assignment, or ternary operators.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <param name="debug">Whether to emit debug output during tokenization or RPN conversion.</param>
        /// <returns>The evaluated numeric result.</returns>
        public double SimpleEvaluate(string expression, bool debug = false)
        {
            var tokens = ToTokenArray(expression, debug);
            var rpn = ConvertToRPN(tokens, debug);
            return EvaluateRPN(rpn);
        }

        private string[] ToTokenArray(string expression, bool debug = false)
        {
            List<string> tokens = [];
            var currentToken = new StringBuilder();
            var previousChar1 = ' ';
            var previousChar2 = ' ';
            int bracketBalance = 0;
            int negativeNumberBalance = 0;
            expression = expression.ToLower();
            for (int i = 0; i < expression.Length; i++)
            {
                var c = expression[i];
                if (char.IsWhiteSpace(c)) continue;
                if (i > 0) previousChar1 = expression[i - 1];
                if (i > 1) previousChar2 = expression[i - 2];
                if (IsBracket(c))
                {
                    if (c == '(' && (IsDigit(previousChar1) || IsDecimal(previousChar1)))
                    {
                        tokens.Add("*");
                    }
                    if (c == '(') bracketBalance++;
                    if (c == ')') bracketBalance--;
                    if (currentToken.Length > 0) tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                    currentToken.Append(c);
                    if (c == '(' && (previousChar1 == ')' || (tokens.Count >= 1 && Constants.ContainsKey(tokens[^1])))) tokens.Add("*");
                    if (c == '(' && previousChar1 == '-' && (IsOperator(previousChar2) || previousChar2 == ' '))
                    {
                        if (tokens.Count > 0) tokens.RemoveAt(tokens.Count - 1);
                        tokens.Add("(");
                        negativeNumberBalance++;
                        tokens.Add("-1");
                        tokens.Add("*");
                        continue;
                    }
                    if (c == ')')
                    {
                        while (negativeNumberBalance > 0)
                        {
                            tokens.Add(")");
                            negativeNumberBalance--;
                        }
                    }
                }
                else if (IsDigit(c))
                {
                    if (IsDigit(previousChar1) || IsDecimal(previousChar1) || ((IsOperator(previousChar2) || IsLetter(previousChar2) || previousChar2 == ' ' || previousChar2 == '(') && previousChar1 == '-'))
                    {
                        currentToken.Append(c);
                    }
                    else
                    {
                        if (currentToken.Length > 0) tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                        currentToken.Append(c);
                    }
                    if (previousChar1 == ')') tokens.Add("*");
                    if (IsLetter(previousChar1))
                    {
                        tokens.Add("(");
                        bracketBalance++;
                    }
                }
                else if (IsDecimal(c))
                {
                    if (IsDigit(previousChar1) || IsDecimal(previousChar1))
                    {
                        currentToken.Append(c);
                    }
                    else
                    {
                        if (currentToken.Length > 0) tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                        currentToken.Append(c);
                    }
                    if (previousChar1 == ')') tokens.Add("*");
                }
                else if (IsOperator(c))
                {
                    if (currentToken.Length > 0) tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                    currentToken.Append(c);
                    if (IsLetter(previousChar1) && Functions.IsValidFunction(tokens[^1]))
                    {
                        tokens.Add("(");
                        bracketBalance++;
                    }
                }
                else if (IsLetter(c))
                {
                    if (IsLetter(previousChar1))
                    {
                        currentToken.Append(c);
                    }
                    else
                    {
                        if (previousChar1 == '-' && (previousChar2 == '(' || previousChar2 == ' ' || IsOperator(previousChar2)))
                        {
                            tokens.Add("(");
                            negativeNumberBalance++;
                            tokens.Add("-1");
                            tokens.Add("*");
                            currentToken.Clear();
                            currentToken.Append(c);
                            continue;
                        }
                        if (currentToken.Length > 0) tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                        currentToken.Append(c);
                    }
                    if (previousChar1 == ')' || IsDigit(previousChar1) || IsDecimal(previousChar1)) tokens.Add("*");
                }
            }
            if (currentToken.Length > 0) tokens.Add(currentToken.ToString());
            int loopDirection = bracketBalance > 0 ? -1 : 1;
            for (int i = bracketBalance + negativeNumberBalance; i != 0; i += loopDirection)
            {
                if (loopDirection < 0)
                {
                    tokens.Add(")");
                }
                if (loopDirection > 0)
                {
                    tokens.Insert(0, "(");
                }
            }
            if (debug)
            {
                Console.WriteLine($"Initial expression: {expression}");
                Console.WriteLine("Tokens: ");
                foreach (var token in tokens)
                {
                    Console.WriteLine(token);
                }
            }
            return [.. tokens];
        }

        private string[] ConvertToRPN(string[] tokens, bool debug = false)
        {
            var output = new List<string>();
            var stack = new Stack<string>();
            foreach (var token in tokens)
            {
                if (debug)
                {
                    Console.WriteLine(token);
                }
                if (double.TryParse(token, out _))
                {
                    output.Add(token);
                }
                else if (IsLetter(token[0]))
                {
                    if (Constants.ContainsKey(token))
                    {
                        if (Constants.TryGetValue(token, out var value))
                        {
                            output.Add($"{value}");
                        }
                        else
                        {
                            stack.Push(token);
                        }
                    }
                    else if (Variables.ContainsKey(token))
                    {
                        if (Variables.TryGetValue(token, out var value))
                        {
                            output.Add($"{value}");
                        }
                        else
                        {
                            stack.Push(token);
                        }
                    }
                    else if (Accessors.ContainsKey(token))
                    {
                        if (Accessors.TryGetValue(token, out var dlgt))
                        {
                            output.Add($"{dlgt.getter()}");
                        }
                        else
                        {
                            stack.Push(token);
                        }
                    }
                    else
                    {
                        stack.Push(token);
                    }
                }
                else if (IsOperator(token))
                {
                    while (stack.Count > 0 && IsOperator(stack.Peek()) &&
                           ((IsLeftAssociative(token) && Precedence(token) <= Precedence(stack.Peek())) ||
                            (!IsLeftAssociative(token) && Precedence(token) < Precedence(stack.Peek()))))
                    {
                        output.Add(stack.Pop());
                    }
                    stack.Push(token);
                }
                else if (token == "(")
                {
                    stack.Push(token);
                }
                else if (token == ")")
                {
                    while (stack.Count > 0 && stack.Peek() != "(")
                    {
                        output.Add(stack.Pop());
                    }
                    if (stack.Count == 0) throw new FormatException("Mismatched parentheses.");
                    stack.Pop(); // discard "("

                    if (stack.Count > 0 && char.IsLetter(stack.Peek()[0]))
                    {
                        output.Add(stack.Pop()); // function call completed
                    }
                }
            }

            while (stack.Count > 0)
            {
                var op = stack.Pop();
                if (op == "(" || op == ")") throw new FormatException("Mismatched parentheses.");
                output.Add(op);
            }

            if (debug)
            {
                Console.WriteLine("Reverse Polish Notation:");
                foreach (var token in output)
                {
                    Console.WriteLine(token);
                }
            }
            return [.. output];
        }

        private double EvaluateRPN(string[] rpn)
        {
            var stack = new Stack<double>();
            foreach (var token in rpn)
            {
                if (double.TryParse(token, out var result))
                {
                    stack.Push(result);
                }
                if (Functions.IsValidFunction(token))
                {
                    double value = stack.Pop();
                    if (Functions.TryFunction(token, value, out result))
                        stack.Push(result);
                    else
                    {
                        throw new Exception("How the hell did you reach here?");
                    }
                }
                if (IsOperator(token))
                {
                    double rightArg = stack.Pop();
                    double leftArg = stack.Pop();
                    switch (token)
                    {
                        case "+":
                            stack.Push(leftArg + rightArg);
                            break;
                        case "-":
                            stack.Push(leftArg - rightArg);
                            break;
                        case "*":
                            stack.Push(leftArg * rightArg);
                            break;
                        case "/":
                            stack.Push(leftArg / rightArg);
                            break;
                        case "^":
                            stack.Push(Math.Pow(leftArg, rightArg));
                            break;
                        case "%":
                            stack.Push(leftArg % rightArg);
                            break;
                        case "&":
                            stack.Push(((leftArg != 0) && (rightArg != 0)) ? 1 : 0);
                            break;
                        case "|":
                            stack.Push(((leftArg != 0) || (rightArg != 0)) ? 1 : 0);
                            break;
                        case "$":
                            stack.Push(((leftArg != 0) ^ (rightArg != 0)) ? 1 : 0);
                            break;
                    }
                }
            }
            if (stack.Count != 1)
            {
                throw new FormatException("stack count is not 1. Invalid format used");
            }
            return stack.Pop();
        }

        private static bool IsOperator(char c)
        {
            return "+-×*/÷^%|&$".Contains(c);
        }

        private static bool IsOperator(string op)
        {
            return op.Length == 1 && IsOperator(op[0]);
        }

        private static bool IsDigit(char c)
        {
            return char.IsDigit(c);
        }

        private static bool IsDecimal(char c)
        {
            return c == '.';
        }

        private static bool IsLetter(char c)
        {
            return char.IsLetter(c);
        }

        private static bool IsBracket(char c)
        {
            return c == '(' || c == ')';
        }

        private static int Precedence(string op)
        {
            switch (op)
            {
                case "|":
                    return 1;
                case "&":
                    return 2;
                case "$":
                    return 3;
                case "+" or "-":
                    return 4;
                case "*" or "/" or "%":
                    return 5;
                case "^":
                    return 6;
                default:
                    return 0;
            }
        }

        private static bool IsLeftAssociative(string op)
        {
            switch (op)
            {
                case "^":
                    return false;
                default:
                    return true;
            }
        }
    }
}

/// <summary>
/// Provides a lookup table of mathematical and logical functions supported by LineDSL,
/// allowing dynamic evaluation of unary operations by name.  
/// Functions are stored as <see cref="Func{Double, Double}"/> delegates and
/// can be queried or invoked through <see cref="TryFunction"/>.
/// </summary>
public static class Functions
{
    /// <summary>
    /// Internal dictionary mapping function names (lowercase) to their corresponding delegates.
    /// </summary>
    private static Dictionary<string, Func<double, double>> _functions = new()
    {
        {Sin, Math.Sin},
        {Cos, Math.Cos},
        {Tan, Math.Tan},
        {ASin, Math.Asin},
        {ACos, Math.Acos},
        {ATan, Math.Atan},
        {Sinh, Math.Sinh},
        {Cosh, Math.Cosh},
        {Tanh, Math.Tanh},
        {ASinh, Math.Asinh},
        {ACosh, Math.Acosh},
        {ATanh, Math.Atanh},
        {Log10, Math.Log10},
        {LogN, Math.Log},
        {Log2, Math.Log2},
        {Sqrt, Math.Sqrt},
        {Exp, Math.Exp},
        {Abs, Math.Abs},
        {Floor, Math.Floor},
        {Ceil, Math.Ceiling},
        {Round, x => Math.Round(x, MidpointRounding.AwayFromZero)},
        {Sign, x => Math.Sign(x)},
        {Eqz, x => x == 0 ? 1 : 0},
        {Neqz, x => x != 0 ? 1 : 0},
        {Geqz, x => x >= 0 ? 1 : 0},
        {Leqz, x => x <= 0 ? 1 : 0},
        {Gtz, x => x > 0 ? 1 : 0},
        {Ltz, x => x < 0 ? 1 : 0},
        {IsNaN, x => double.IsNaN(x) ? 1 : 0},
        {IsInf, x => double.IsInfinity(x) ? 1 : 0},
        {Not, x => x == 0 ? 1 : 0},
        {Bool, x => x != 0 ? 1 : 0},
        {Negate, x => x != 0 ? -1 * x : 0},
        {Print, PrintToConsole},
        {DegToRad, x => MathHelper.DegreesToRadians(x)}
    };

    /// <summary>
    /// Attempts to evaluate a unary function by name.
    /// </summary>
    /// <param name="function">The name of the function (case-insensitive).</param>
    /// <param name="arg">The input argument to the function.</param>
    /// <param name="result">
    /// The output value of the function if found; otherwise <see cref="double.NaN"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the function name exists and was executed;  
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool TryFunction(string function, double arg, out double result)
    {
        if (_functions.TryGetValue(function.ToLower(), out var func))
        {
            result = func(arg);
            return true;
        }
        result = double.NaN;
        return false;
    }

    /// <summary>
    /// Determines whether a function name corresponds to a valid registered function.
    /// </summary>
    /// <param name="name">The name of the function (case-insensitive).</param>
    /// <returns>
    /// <c>true</c> if the function exists; otherwise <c>false</c>.
    /// </returns>
    public static bool IsValidFunction(string name)
    {
        return _functions.ContainsKey(name.ToLower());
    }

    /// <summary>
    /// Prints a value to the console and returns it unchanged.
    /// Useful for debugging expression systems.
    /// </summary>
    private static double PrintToConsole(double value)
    {
        Console.WriteLine($"{value}");
        return value;
    }

    //trigonometry functions
    /// <summary>Represents the sine function.</summary>
    public const string Sin = "sin";
    /// <summary>Represents the cosine function.</summary>
    public const string Cos = "cos";
    /// <summary>Represents the tangent function.</summary>
    public const string Tan = "tan";
    /// <summary>Represents the arcsine function.</summary>
    public const string ASin = "asin";
    /// <summary>Represents the arccosine function.</summary>
    public const string ACos = "acos";
    /// <summary>Represents the arctangent function.</summary>
    public const string ATan = "atan";
    /// <summary>Represents the hyperbolic sine function.</summary>
    public const string Sinh = "sinh";
    /// <summary>Represents the hyperbolic cosine function.</summary>
    public const string Cosh = "cosh";
    /// <summary>Represents the hyperbolic tangent function.</summary>
    public const string Tanh = "tanh";
    /// <summary>Represents the inverse huperbolic sine function.</summary>
    public const string ASinh = "asinh";
    /// <summary>Represents the inverse hyperbolic cosine function.</summary>
    public const string ACosh = "acosh";
    /// <summary>Represents the inverse hyperbolic tangent function.</summary>
    public const string ATanh = "atanh";
    /// <summary>Converts degrees to radians.</summary>
    public const string DegToRad = "degtorad";
    //logorithmic functions
    /// <summary>Represents base-10 logarithm.</summary>
    public const string Log10 = "logten";
    /// <summary>Represents natural logarithm.</summary>
    public const string LogN = "ln";
    /// <summary>Represents base-2 logarithm.</summary>
    public const string Log2 = "logtwo";
    //general functions
    /// <summary>Represents square root.</summary>
    public const string Sqrt = "sqrt";
    /// <summary>Represents the exponential function.</summary>
    public const string Exp = "exp";
    /// <summary>Represents the absolute value.</summary>
    public const string Abs = "abs";
    /// <summary>Represents the floor function.</summary>
    public const string Floor = "floor";
    /// <summary>Represents the ceiling function.</summary>
    public const string Ceil = "ceil";
    /// <summary>Represents rounding away from zero.</summary>
    public const string Round = "round";
    /// <summary>Returns the sign of a value.</summary>
    public const string Sign = "sign";
    /// <summary>Prints a value to the console.</summary>
    public const string Print = "print";
    //boolean functions
    /// <summary>Returns 1 if x is equal to 0. Otherwise returns 0.</summary>
    public const string Eqz = "eqz";
    /// <summary>Returns 1 if x is not equal to 0. Otherwise returns 0.</summary>
    public const string Neqz = "neqz";
    /// <summary>Returns 1 if x is greater than or equal to 0. Otherwise returns 0.</summary>
    public const string Geqz = "geqz";
    /// <summary>Returns 1 if x is less than or equal to 0. Otherwise returns 0.</summary>
    public const string Leqz = "leqz";
    /// <summary>Returns 1 if x is greater than 0. Otherwise returns 0.</summary>
    public const string Gtz = "gtz";
    /// <summary>Returns 1 if x is less than 0. Otherwise returns 0.</summary>
    public const string Ltz = "ltz";
    /// <summary>Returns 1 if the input is NaN.</summary>
    public const string IsNaN = "isnan";
    /// <summary>Returns 1 if the input is ±infinity.</summary>
    public const string IsInf = "isinf";
    /// <summary>Logical not: returns 1 if x == 0, otherwise 0.</summary>
    public const string Not = "not";
    /// <summary>Returns 1 if x != 0, otherwise 0.</summary>
    public const string Bool = "bool";
    /// <summary>Negates the value or returns 0 if the value is zero.</summary>
    public const string Negate = "negate";
}