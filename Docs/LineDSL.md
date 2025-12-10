# LineDSL

LineDSL is a lightweight expression engine for evaluating mathematical and logical expressions at runtime. It allows users to:

1. Use constants `(pi, e, true, false)`

2. Create and assign variables `(var x = 10)`

3. Bind external data through lambda accessors (AddLambdaRef)

4. Evaluate expressions with full operator precedence, ternary logic, and function calls

5. Perform lightweight scripting inside UI components, game logic, animations, or user-defined scripts

- Note: LineDSL does not depend on external scripting engines and is self-contained.

## Namespaces in LineDSL

LineDSL maintains three separate namespaces:

### Constants

- Predefined immutable values like pi, tau, phi, e, true, and false.

### Variables

- User-defined variables using var name = expr. Variables can be reassigned later using name = expr.

### Accessors

- Dynamic references bound to external getters and setters via AddLambdaRef(string, (Func<double>, Action<double>)). Useful for linking expressions to live game or UI data.

- Creating Variables

```csharp
var dsl = new LineDSL();

// Creating a variable
dsl.Evaluate("var x = 10");

// Updating an existing variable
dsl.Evaluate("x = x * 2");
```

## Rules:

1. Cannot redefine an existing variable.

2. Cannot assign to constants.

3. Must assign to previously defined variable or accessor.

### Lambda Accessors

Lambda accessors let expressions interact with external data:

```csharp
double externalValue = 5;
dsl.AddLambdaRef("external", (
    getter: () => externalValue,
    setter: val => externalValue = val
));

// Now you can use it in expressions:
dsl.Evaluate("external = external * 2"); // externalValue becomes 10
```

- Use `RemoveLambdaRef(name)` to remove a binding.

- Use `ClearLambdaRefs()` to remove all bindings.

## Evaluating Expressions

### Simple Evaluation

For expressions without variables or assignment:

```csharp
double result = dsl.SimpleEvaluate("3 + sin(pi / 2)");
```

- Supports arithmetic operators: + - \* / ^ %

- Supports logical operators: & | $

- Supports unary functions (see Functions below)

### Full Evaluation

Includes assignment, ternary operators, and variables:

```csharp
double result = dsl.Evaluate("x = x > 5 ? x * 2 : x / 2");
```

- Ternary operator: condition ? trueExpr : falseExpr

- Assignment operator: =

- var keyword for creating variables

## Functions

These are the names of the supported functions in LineDSL. Functions are case insensitive, though behind the scenes they are treated as lower case.

- sin: The sine function.

- cos: The cosine function.

- tan: The tangent function.

- asin: The inverse sine function.

- acos: The inverse cosine function.

- atan: The inverse tangent function.

- sinh: The hyperbolic sine function.

- cosh: The hyperbolic cosine function.

- tanh: The hyperbolic tangent function.

- asinh: The inverse hyperbolic sine function.

- acosh: The inverse hyperbolic cosine function.

- atanh: The inverse hyperbolic tangent function.

- degtorad: Converts a given value from degrees to radians.

- logten: The base 10 logarithm function.

- ln: The natural logarithm function.

- logtwo: The base 2 logarithm function.

- sqrt: The square root function.

- exp: The exponential function. Can also be expressed as x^y.

- abs: The absolute function.

- floor: The floor function. Returns the value of x rounded down.

- ceil: The ceiling function. Returns the value of x rounded up.

- round: The rounding function. Returns the value of the nearest integer to x.

- sign: The sign function. Returns -1 if x is less than 0, 0 if x is equal to 0 or 1 if x is greater than 0.

- print: Prints the evaluated result to the console.

- eqz: Stands for Equal to Zero. If x is equal to 0, returns 1 otherwise returns 0.

- neqz: Stands for Not Equal to Zero. If x is not equal to 0, returns 1 otherwise returns 0.

- geqz: Stands for Greater than or Equal to Zero. If x is greater than or equal to 0, returns 1 otherwise returns 0.

- leqz: Stands for Less than or Equal to Zero. If x is less than or equal to 0, returns 1 otherwise returns 0.

- gtz: Stands for Greater than Zero. If x is greater than 0, returns 1 otherwise returns 0.

- ltz: Stands for Less than Zero. If x is less than 0, returns 1 otherwise returns 0.

- isnan: Checks if the given value is a valid number. Returns 1 if x is not a number, otherwise returns 0.

- isinf: Checks if the given value is positive or negative infinity. Returns 1 if x is positive or negative infinity otherwise returns 0.

- not: The not function. If x is equal to zero, returns 1 otherwise returns 0.

- bool: The bool function. Converts a value to either 1 (true) or 0 (false). Any number that is not zero is considered true.

- negate: The negation function. Converts a positive value to a negative equivalent and vice versa.

## Error Handling

- Assigning to a constant throws FormatException.

- Re-defining an existing variable throws FormatException.

- Invalid ternary syntax (? without :) throws FormatException.

- Stack errors in RPN evaluation throw FormatException.
