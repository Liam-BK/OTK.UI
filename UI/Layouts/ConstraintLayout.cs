using System.Xml.Linq;
using OTK.UI.Utility;

namespace OTK.UI.Layouts
{
    /// <summary>
    /// A layout that positions UI elements based on a set of constraints expressed
    /// as strings, evaluated by a <see cref="LineDSL"/> solver.
    /// </summary>
    public class ConstraintLayout : Layout
    {

        /// <summary>
        /// Loads a <see cref="ConstraintLayout"/> from an XML element.
        /// Expects one or more &lt;Constraint&gt; child elements containing the
        /// constraint expressions as strings.
        /// </summary>
        /// <param name="element">The XML element defining the layout.</param>
        /// <returns>A fully initialized <see cref="ConstraintLayout"/> instance.</returns>
        public static new ConstraintLayout Load(XElement element)
        {
            var layout = new ConstraintLayout();
            var constraints = new List<string>();
            foreach (var constraint in element.Elements("Constraint"))
            {
                constraints.Add(constraint.Value);
            }
            layout.Constraints = constraints;
            return layout;
        }

        /// <summary>
        /// The list of constraint expressions to evaluate for positioning elements.
        /// Each string should be a valid expression understood by the <see cref="LineDSL"/> solver.
        /// </summary>
        public List<string> Constraints = new();

        /// <summary>
        /// The instance of <see cref="LineDSL"/> used to evaluate the constraint expressions.
        /// </summary>
        public LineDSL LineDSLInstance = new();

        /// <summary>
        /// Applies all constraints to the layout by evaluating each expression
        /// in <see cref="Constraints"/> using <see cref="LineDSLInstance"/>.
        /// Exceptions during evaluation are caught and logged to the console.
        /// </summary>
        public override void Apply()
        {
            foreach (var constraint in Constraints)
            {
                try
                {
                    LineDSLInstance?.Evaluate(constraint);
                }
                catch (Exception E)
                {
                    Console.WriteLine($"{E.Message} constraint: {constraint}");
                }
            }
        }
    }
}