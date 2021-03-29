using System;
using System.Collections.Generic;

namespace TreeAnnotation
{
    public class Tao
    {
        static Part other = new Other();
        public static Tao parse(string str) {
            return tao(new Input(str));
        }
        static Tao tao(Input input) {
            var tao = new Tao();
            while (true) {
                if (input.atBound()) return tao;
                var part = tree(input);
                if (part.isOther()) {
                    part = op(input);
                    if (part.isOther()) {
                        part = note(input);
                    }
                }
                tao.push(part);
            }
        }
        static Part tree(Input input) {
            if (input.at('[')) {
                input.next();
                input.bound(']');
                var tree = tao(input);
                input.unbound();
                input.next();
                return new Tree(tree);
            }
            return other;
        }
        static Part op(Input input) {
            if (input.at('`')) {
                input.next();
                if (input.done()) input.error("op (unexpected end of input)");
                return new Op(input.next());
            }
            return other;
        }
        static Part note(Input input) {
            if (meta(input)) input.error("note (unexpected meta symbol)");
            string note = "" + input.next();
            while (true) {
                if (input.done() || meta(input)) return new Note(note);
                note += input.next();
            }
        }
        static bool meta(Input input) {
            return input.at('[') || input.at('`') || input.at(']');
        }

        // todo: limit parts to {get;}
        public List<Part> parts = new List<Part>();
        public void push(Part part) {
            parts.Add(part);
        }
        override public string ToString() {
            var str = "";
            foreach (Part p in parts) {
                str += p.ToString();
            }
            return str;
        }
    }

    public class Part {
        virtual public bool isTree() {return false;}
        virtual public bool isOp() {return false;}
        virtual public bool isNote() {return false;}

        virtual public bool isOther() {return false;}

        virtual public Tree asTree() {throw new System.Exception("Not a Tree!");}
        virtual public Op asOp() {throw new System.Exception("Not an Op!");}
        virtual public Note asNote() {throw new System.Exception("Not a Note!");}
    }
    public class Tree: Part {
        // tao
        public Tao tao {get;}
        override public bool isTree() {return true;}
        override public Tree asTree() {return this;}
        public Tree(Tao tree) {
            this.tao = tree;
        }
        override public string ToString() {
            return "[" + tao + "]";
        }
    }
    public class Note: Part {
        // 1*any-except-meta -- symbols
        public string syms {get;}
        override public bool isNote() {return true;}
        override public Note asNote() {return this;}
        public Note(string note) {
            this.syms = note;
        }
        override public string ToString() {
            return syms;
        }
    }
    public class Op: Part {
        // any -- symbol
        public char sym {get;}
        override public bool isOp() {return true;}
        override public Op asOp() {return this;}
        public Op(char op) {
            this.sym = op;
        }
        override public string ToString() {
            return "`" + sym;
        }
    }
    public class Other: Part {
        override public bool isOther() {return true;}
    }
    class Input {
        int length;
        int position = 0;
        int line = 0;
        int column = 0;
        string str;
        Stack<Bound> bounds = new Stack<Bound>();
        public Input(string str) {
            this.str = str;
            this.length = str.Length;
        }
        public bool done() { return position >= length; }
        public bool at(char symbol) { return str[position] == symbol; }
        public char next() { 
            char c = str[position++]; 
            if (c == '\n') {
                this.line += 1;
                this.column = 0;
            } else {
                this.column += 1;
            }
            return c;
        }
        private char peek() {
            return str[position];
        }
        public void error(string name) {
            throw new System.Exception(
                line + ":" + column + ": malformed " + name + 
                " at line " + line + ", column " + column + 
                " (position " + position + ")."
            );
        }
        public void bound(char symbol) {
            Bound b = new Bound();
            b.position = position;
            b.line = line;
            b.column = column;
            b.symbol = symbol;
            bounds.Push(b); 
        }
        public void unbound() { bounds.Pop(); }
        public bool atBound() {
            if (bounds.Count > 0) {
                var b = bounds.Peek();
                if (done()) throw new System.Exception(
                    line + ":" + column + ": expected symbol " + b.symbol +
                    " before the end of input since line " + b.line + ", column " + b.column + 
                    " (position " + b.position + ")."
                );
                return at(b.symbol);
            }
            return done();
        }
        class Bound {
            public int position;
            public int line;
            public int column;
            public char symbol;
        }
    }
}
