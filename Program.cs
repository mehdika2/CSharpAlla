using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CSharpAlla
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string code = @"
mybool = true

myfloat = (1 - .5154) + ((.5154 * 2) / 2)

num1 = 10
num2 = 20
result = num1 * 4 + (num2 + 9 / num1 * 5)

name = ""mahdi""
family = ""khalilzadeh""
fullname = name + "" "" + family + "" Your: "" + result + "" Years Old."" + mybool 

s = myfloat + 0.4846 + 1 + ""!""

num3 = 10 -- 20 
num3 = num3 + 15

result = ""Hel"" + ""l"" + ""o""

result2 = num3 == num1 + num2 + (30 / 2) & num3 - 5 == num1 * 4 & result == ""Hell"" + ""o""
";

            long totalMiliseconds = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Lexer lexer = new Lexer(code);
            List<Token> tokens = lexer.Tokenize();

            Console.WriteLine("[Lexer Finish] " + sw.ElapsedMilliseconds + "ms");
            totalMiliseconds += sw.ElapsedMilliseconds;
            sw.Reset();
            sw.Start();

            Parser parser = new Parser(tokens);
            parser.Parse();

            Console.WriteLine("[Parser Finish] " + sw.ElapsedMilliseconds + "ms");
            totalMiliseconds += sw.ElapsedMilliseconds;
            sw.Reset();
            sw.Start();

            Interpreter interpreter = new Interpreter();
            Console.WriteLine(interpreter.Interpret(parser));

            Console.WriteLine("[Interpreter Finish] " + sw.ElapsedMilliseconds + "ms");
            totalMiliseconds += sw.ElapsedMilliseconds;
            sw.Reset();

            Console.Title = totalMiliseconds + "ms";

            Console.WriteLine(parser.constants.Count + " Constrator");
            Console.WriteLine(parser.variables.Count + " Variables");
            Console.WriteLine(parser.bytecodes.Count + " Bytecodes");

            //Console.WriteLine("========= Disassembled =========");

            //Console.WriteLine(interpreter.Disassemble(parser));
        }
    }
}
