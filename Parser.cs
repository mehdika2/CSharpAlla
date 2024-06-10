using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CSharpAlla
{
    public class Parser
    {
        private List<Token> tokens;
        public List<object> constants = new List<object>();
        public List<string> variables = new List<string>();
        public List<byte> bytecodes = new List<byte>();
        private int position = 0;
        private Token currentToken;

        public List<Token> Tokens
        {
            get
            {
                return tokens;
            }

            set
            {
                tokens = value;
            }
        }

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
            currentToken = tokens[position];
        }

        public void Parse()
        {
            bytecodes = new List<byte>();

            while (!IsAtEnd())
            {
                if (Match(TokenType.Identifier))
                {
                    VariableDeclaration();
                }
                else
                {
                    Expression();
                }
            }
        }

        private void VariableDeclaration()
        {
            var varName = Previous().Value.ToString();
            Eat(TokenType.Assign);

            int varIndex = GetVariableIndex(varName);
            Expression();
            bytecodes.Add(0x03); // StoreVar
            bytecodes.Add((byte)varIndex);
        }

        private void Expression()
            => OrExpression();

        private void OrExpression()
        {
            AndExpression();

            while (Match(TokenType.Or))
            {
                bytecodes.Add(0x07); // JumpIfTrueOrPop
                int jumpPos = bytecodes.Count;
                bytecodes.Add(0); // Placeholder for jump offset

                AndExpression();

                bytecodes[jumpPos] = (byte)(bytecodes.Count - jumpPos - 1);
            }
        }

        private void AndExpression()
        {
            EqualityExpression();
            while (Match(TokenType.And))
            {
                bytecodes.Add(0x08); // JumpIfFalseOrPop
                int jumpPos = bytecodes.Count;
                bytecodes.Add(0); // Placeholder for jump offset

                EqualityExpression();

                bytecodes[jumpPos] = (byte)(bytecodes.Count - jumpPos - 1);
            }
        }

        private void EqualityExpression()
        {
            AdditiveExpression();
            while (Match(TokenType.Equal, TokenType.Unequal))
            {
                var opCode = GetOperationCode(Previous().Type);
                AdditiveExpression();
                bytecodes.Add(0x04); // CompareOp
                bytecodes.Add(opCode);
            }
        }

        private void AdditiveExpression()
        {
            MultiplicativeExpression();
            while (Match(TokenType.Plus, TokenType.Minus))
            {
                var opCode = GetBinaryCode(Previous().Type);
                MultiplicativeExpression();
                bytecodes.Add(0x05); // BinaryOp
                bytecodes.Add(opCode);
            }
        }

        private void MultiplicativeExpression()
        {
            UnaryExpression();
            while (Match(TokenType.Multiply, TokenType.Divide))
            {
                var opCode = GetBinaryCode(Previous().Type);
                UnaryExpression();
                bytecodes.Add(0x05); // BinaryOp
                bytecodes.Add(opCode);
            }
        }
        
        private void UnaryExpression()
        {
            if (Match(TokenType.Minus))
            {
                PrimaryExpression();
                bytecodes.Add(0x06); // unary variable
                bytecodes.Add(0x00); // skip

                if(currentToken.Type != TokenType.Identifier)
                    Advance();
            }
            else
            {
                PrimaryExpression();
            }
        }

        private void PrimaryExpression()
        {
            Token token = currentToken;
            switch (token.Type)
            {
                case TokenType.Number:
                    Eat(TokenType.Number);
                    bytecodes.Add(0x01); // Load_Const
                    if (token.Value.ToString().Contains('.'))
                        bytecodes.Add(GetConstantIndex(double.Parse(token.Value.ToString())));
                    else bytecodes.Add(GetConstantIndex(int.Parse(token.Value.ToString())));
                    break;

                case TokenType.Identifier:
                    Eat(TokenType.Identifier);
                    bytecodes.Add(0x02); // LoadVar
                    bytecodes.Add(GetVariableIndex((string)token.Value));
                    break;

                case TokenType.LeftParen:
                    Eat(TokenType.LeftParen);
                    Expression();
                    Eat(TokenType.RightParen);
                    break;

                case TokenType.Bool:
                    Eat(TokenType.Bool);
                    bytecodes.Add(0x01); // LoadConst
                    bytecodes.Add(GetConstantIndex(bool.Parse((string)token.Value)));
                    break;

                case TokenType.String:
                    Eat(TokenType.String);
                    bytecodes.Add(0x01); // LoadConst
                    bytecodes.Add(GetConstantIndex(token.Value));
                    break;

                default:
                    throw new Exception($"Unexpected token {token.Type}.");
            }
        }

        private byte GetConstantIndex(object value)
        {
            int index = constants.IndexOf(value);
            if (index == -1)
            {
                constants.Add(value);
                index = constants.Count - 1;
            }
            return (byte)index;
        }

        private byte GetVariableIndex(string varName)
        {
            int index = variables.IndexOf(varName);
            if (index == -1)
            {
                variables.Add(varName);
                index = variables.Count - 1;
            }
            return (byte)index;
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return currentToken.Type == type;
        }

        private void Eat(TokenType type)
        {
            if (currentToken.Type == type)
                Advance();
            else
                throw new Exception($"Expected token {type}, got {currentToken.Type}");
        }

        private void Advance()
        {
            position++;
            if (IsAtEnd()) return;
            currentToken = tokens[position];
        }

        /// <summary>
        /// Get previous token after match
        /// </summary>
        private Token Previous()
        {
            return tokens[position - 1];
        }

        /// <summary>
        /// Get operation codes for 
        /// </summary>
        private byte GetOperationCode(TokenType type)
        {
            switch (type)
            {
                case TokenType.Equal: return 0x10;
                case TokenType.Unequal: return 0x11;
                default: throw new Exception("Unknown operation");
            };
        }

        private byte GetBinaryCode(TokenType type)
        {
            switch (type)
            {
                case TokenType.Plus: return 0x20;
                case TokenType.Minus: return 0x21;
                case TokenType.Multiply: return 0x22;
                case TokenType.Divide: return 0x23;
                default: throw new Exception("Unknown operation");
            };
        }

        private bool IsAtEnd()
        {
            return position >= tokens.Count;
        }




        ////// Debug
        private Token _Previous
        {
            get
            {
                return tokens[position - 1];
            }
        }
    }
}
