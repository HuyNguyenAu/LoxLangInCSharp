﻿using System;
using System.Collections.Generic;

namespace LoxLangInCSharp
{
    public class Parser
    {
        private class ParseError : Exception { }

        private readonly List<Token> tokens;
        private int current = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public List<Statement> Parse()
        {
            List<Statement> statements = new List<Statement>();

            while (!IsAtEnd())
            {
                statements.Add(Declaration());
            }

            return statements;
        }

        private Expression Expression()
        {
            return Assignment();
        }

        private Statement Declaration()
        {
            try
            {
                if (Match(TokenType.VAR)) return VarDeclaration();

                return Statement();
            }
            catch (ParseError)
            {
                Synchronise();
                return null;
            }
        }

        private Statement Statement()
        {
            if (Match(TokenType.PRINT)) return PrintStatement();
            if (Match(TokenType.LEFT_BRACE)) return new Statement.Block(Block());

            return ExpressionStatement();
        }

        private Statement PrintStatement()
        {
            Expression value = Expression();
            Consume(TokenType.SEMICOLON, "Expected ';' after value");
            return new Statement.Print(value);
        }

        private Statement VarDeclaration()
        {
            Token name = Consume(TokenType.IDENTIFIER, "Expect variable name.");
            Expression initialiser = null;
            
            if (Match(TokenType.EQUAL))
            {
                initialiser = Expression();
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");

            return new Statement.Var(name, initialiser);
        }

        private Statement ExpressionStatement()
        {
            Expression value = Expression();
            Consume(TokenType.SEMICOLON, "Expected ';' after expression.");
            return new Statement.Expression(value);
        }

        private List<Statement> Block()
        {
            List<Statement> statements = new List<Statement>();

            while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                statements.Add(Declaration());
            }

            Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");

            return statements;
        }

        private Expression Assignment()
        {
            Expression expression = Equality();

            if (Match(TokenType.EQUAL))
            {
                Token equals = Previous();
                Expression value = Assignment();

                if (expression.GetType() == typeof(Expression.Variable))
                {
                    Token name  = (expression as Expression.Variable).name;
                    return new Expression.Assign(name, value);
                }

                Error(equals, "Invalid assignment target.");
            }

            return expression;
        }

        private Expression Equality()
        {
            Expression left = Comparison();

            while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
            {
                Token op = Previous();
                Expression right = Comparison();
                left = new Expression.Binary(left, op, right);
            }

            return left;
        }

        private Expression Comparison()
        {
            Expression left = Term();


            while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                Token op = Previous();
                Expression right = Term();
                left = new Expression.Binary(left, op, right);
            }

            return left;
        }

        private Expression Term()
        {
            Expression left = Factor();


            while (Match(TokenType.MINUS, TokenType.PLUS))
            {
                Token op = Previous();
                Expression right = Factor();
                left = new Expression.Binary(left, op, right);
            }

            return left;
        }

        private Expression Factor()
        {
            Expression left = Unary();


            while (Match(TokenType.SLASH, TokenType.STAR))
            {
                Token op = Previous();
                Expression right = Unary();
                left = new Expression.Binary(left, op, right);
            }

            return left;
        }

        private Expression Unary()
        {
            while (Match(TokenType.BANG, TokenType.MINUS))
            {
                Token op = Previous();
                Expression right = Unary();
                return new Expression.Unary(op, right);
            }

            return Primary();
        }

        private Expression Primary()
        {
            if (Match(TokenType.FALSE)) return new Expression.Literal(false);
            if (Match(TokenType.TRUE)) return new Expression.Literal(true);
            if (Match(TokenType.NIL)) return new Expression.Literal(null);

            if (Match(TokenType.NUMBER, TokenType.STRING))
            {
                return new Expression.Literal(Previous().literal);
            }

            if (Match(TokenType.IDENTIFIER))
            {
                return new Expression.Variable(Previous());
            }

            if (Match(TokenType.LEFT_PAREN))
            {
                Expression expression = Expression();
                Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");

                return new Expression.Grouping(expression);
            }

            throw Error(Peek(), "Expected expression.");
        }

        private bool Match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(Peek(), message);
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().type == TokenType.EOF;
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private Token Previous()
        {
            return tokens[current - 1];
        }

        private ParseError Error(Token token, string message)
        {
            ReportError(token, message);
            return new ParseError();
        }

        private static void ReportError(Token token, string message)
        {
            if (token.type == TokenType.EOF)
            {
                Report(token.line, " at end", message);
            }
            else
            {
                Report(token.line, " at '" + token.lexeme + "'", message);
            }
        }

        private static void Report(int line, string where, string message)
        {
            Console.Error.WriteLine("[line " + line + "] Error" + where + ": " + message);
        }

        private void Synchronise()
        {
            Advance();

            while (!IsAtEnd())
            {
                if (Previous().type == TokenType.SEMICOLON) return;

                switch (Peek().type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                }

                Advance();
            }
        }

    }
}