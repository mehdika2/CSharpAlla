using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpAlla
{
    internal class Interpreter
    {
        Stack<object> stack;
        List<object> variables;
        object[] constants;
        private Dictionary<byte, Action<byte>> bytecodeHandlers;
        Parser parser;
        int position;

        /// <summary>
        /// execute bytes codes
        /// </summary>
        /// <returns>last item in the code stack</returns>
        public object Interpret(Parser parser)
        {
            stack = new Stack<object>();
            variables = new List<object>();

            constants = parser.constants.ToArray();

            this.parser = parser;

            bytecodeHandlers = new Dictionary<byte, Action<byte>>
            {
                { 0x01, Load_Const },
                { 0x02, Load_Var },
                { 0x03, Store_Var },
                { 0x04, Compare_OP },
                { 0x05, Binary_OP },
                { 0x06, Unary_Negative },
                { 0x07, Jump_If_True_Or_Pop },
                { 0x08, Jump_If_False_Or_Pop },
                { 0x09, Pop_Jump_Forward_If_False },
                { 0x10, Pop_Jump_Forward_If_True },
            };

            for (position = 0; position < parser.bytecodes.Count; position++)
            {
                Action<byte> handler;
                if (bytecodeHandlers.TryGetValue(parser.bytecodes[position], out handler))
                    handler(parser.bytecodes[++position]);
                else
                    throw new InvalidOperationException($"Unknown bytecode: {parser.bytecodes[position]}");
            }

            object lastVariable = variables.Reverse<object>().First();
            return $"{lastVariable} ({lastVariable.GetType().Name})";
        }

        public string Disassemble(Parser parser)
        {
            stack = new Stack<object>();
            variables = new List<object>();

            constants = parser.constants.ToArray();

            this.parser = parser;

            bytecodeHandlers = new Dictionary<byte, Action<byte>>
            {
                { 0x01, Load_Const },
                { 0x02, Load_Var },
                { 0x03, Store_Var },
                { 0x04, Compare_OP },
                { 0x05, Binary_OP },
                { 0x06, Unary_Negative },
                { 0x07, Jump_If_True_Or_Pop },
                { 0x08, Jump_If_False_Or_Pop },
                { 0x09, Pop_Jump_Forward_If_False },
                { 0x10, Pop_Jump_Forward_If_True },
            };

            StringBuilder sb = new StringBuilder();
            for (position = 0; position < parser.bytecodes.Count; position++)
            {
                byte ___b = parser.bytecodes[position];
                byte ___n = parser.bytecodes[position + 1];
                Action<byte> handler;
                if (bytecodeHandlers.TryGetValue(parser.bytecodes[position], out handler))
                {
                    int lastPosition = position;
                    handler(parser.bytecodes[++position]);
                    position = ++lastPosition;
                    sb.AppendLine($"{handler.Method.Name}\t{___n} ({GetBytecodeValue(___b, ___n, parser)})");
                }
                else
                    throw new InvalidOperationException($"Unknown bytecode: {parser.bytecodes[position]}");
            }

            return sb.ToString();
        }

        private string GetBytecodeValue(byte action, byte value, Parser parser)
        {
            switch (action)
            {
                case 0x01:
                    return constants[value].ToString();
                case 0x02:
                case 0x03:
                    return parser.variables[value].ToString();
                case 0x04:
                    if (value == 0x10)
                        return "==";
                    else
                        return "!=";
                case 0x05:
                    if (value == 0x20)
                        return "+";
                    else if (value == 0x21)
                        return "-";
                    else if (value == 0x22)
                        return "*";
                    else
                        return "/";
                case 0x07:
                case 0x08:
                    return (value / 2).ToString();
                default:
                    return "";
            }
        }

        private void Load_Const(byte code)
        {
            stack.Push(constants[code]);
        }

        private void Load_Var(byte code)
        {
            if (code >= variables.Count)
                throw new Exception($"'{parser.variables[code]}' is not defined");
            stack.Push(variables[code]);
        }

        private void Store_Var(byte code)
        {
            if (code >= variables.Count)
            {
                variables.Add(stack.Pop());
                return;
            }
            variables[code] = stack.Pop();
        }

        private void Compare_OP(byte code)
        {
            object e1 = stack.Pop();
            object e2 = stack.Pop();

            if (IsNumberType(e1) && IsNumberType(e2))
            {
                double d1 = Convert.ToDouble(e1);
                double d2 = Convert.ToDouble(e2);

                if (code == 0x10)
                {
                    stack.Push(d1 == d2);
                }
                else
                {
                    stack.Push(d1 != d2);
                }
            }
            else
            {
                if (code == 0x10)
                {
                    stack.Push(e1.Equals(e2));
                }
                else
                {
                    stack.Push(!e1.Equals(e2));
                }
            }
        }

        private void Binary_OP(byte code)
        {
            object e2 = stack.Pop();
            object e1 = stack.Pop();
            if (IsNumberType(e1) && IsNumberType(e2))
            {
                double num1 = Convert.ToDouble(e1);
                double num2 = Convert.ToDouble(e2);
                switch (code)
                {
                    case 0x20:
                        stack.Push(num1 + num2);
                        break;
                    case 0x21:
                        stack.Push(num1 - num2);
                        break;
                    case 0x22:
                        stack.Push(num1 * num2);
                        break;
                    default:
                        stack.Push(num1 / num2);
                        break;
                }
            }
            else if (e1 is string || e2 is string)
                stack.Push(e1.ToString() + e2);
            else
                throw new Exception($"Cant do binary operation on '{e1.GetType().Name}' and '{e2.GetType().Name}'");
        }

        private void Unary_Negative(byte code)
        {
            object num = stack.Pop();
            if (num is int)
                stack.Push(-(int)num);
            else if (num is float)
                stack.Push(-(float)num);
            else stack.Push(-(double)num);
        }

        private void Jump_If_True_Or_Pop(byte code)
        {
            if ((bool)stack.Peek())
            {
                position += code;
                return;
            }
            stack.Pop();
        }

        private void Jump_If_False_Or_Pop(byte code)
        {
            if (!(bool)stack.Peek())
            {
                position += code;
                return;
            }
            stack.Pop();
        }

        private void Pop_Jump_Forward_If_True(byte code)
        {
            if (!(bool)stack.Pop())
                position += code;
        }

        private void Pop_Jump_Forward_If_False(byte code)
        {
            if ((bool)stack.Pop())
                position += code;
        }

        static bool IsNumberType(object obj)
        {
            var s = obj.GetType();
            return obj is int || obj is float || obj is double || obj is decimal;
        }
    }
}
