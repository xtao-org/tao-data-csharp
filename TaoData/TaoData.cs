using System;
using System.Collections.Generic;

namespace TreeAnnotation
{
    public class TaoData
    {
        static TaoEmpty emptiness = new TaoEmpty();
        public static TaoData parse(string str) {
            return infer(Tao.parse(str).parts);
        }
        static TaoData infer(List<Part> parts) {
            if (parts.Count == 0) return emptiness;

            var index = Lib.firstIndexOfTree(parts);
            if (index == -1) return new TaoString(str(parts));

            var slice = Lib.slice(parts, 0, index);
            if (Lib.isBlank(slice)) {
                var items = new List<TaoData>();
                foreach (var tao in taos(parts)) {
                    items.Add(infer(tao));
                }
                return new TaoList(items);
            }
            
            var entries = new List<TaoEntry>();
            foreach (var entry in taoEntries(parts)) {
                entries.Add(new TaoEntry(entry.key, infer(entry.parts)));
            }
            return new TaoTable(entries);
        }
        static string str(List<Part> parts) {
            var slices = new List<string>();
            foreach (var part in parts)
            {
                if (part.isNote()) slices.Add(part.asNote().syms);
                else if (part.isOp()) {
                    var op = part.asOp().sym;
                    if (Lib.isValidOp(op)) slices.Add(op.ToString());
                    else throw new System.Exception("Invalid op in a string: " + op);
                } else throw new System.Exception("Not allowed in a string: " + part);
            }
            return string.Join("", slices);
        }
        static List<List<Part>> taos(List<Part> parts) {
            var taos = new List<List<Part>>();
            foreach (var part in parts) {
                if (part.isTree()) taos.Add(part.asTree().tao.parts);
                else if (!Lib.isBlankNote(part)) throw new System.Exception(
                    "Unexpected " + part + " between items. Only whitespace allowed."
                );
            }
            return taos;
        }
        static List<TaoRawEntry> taoEntries(List<Part> parts) {
            var entries = new List<TaoRawEntry>();
            var startIndex = 0;

            while (true) {
                var flat = nextFlat(parts, startIndex);

                if (flat.isLast()) {
                    if (Lib.isBlank(flat.slice)) return entries;
                    else throw new System.Exception(
                        "Only whitespace allowed after entries, got: " + flat
                    );
                }

                var (entry, nextIndex) = taoEntry(parts, flat.asNext());
                entries.Add(entry);
                startIndex = nextIndex;
            }
        }

        static Flat nextFlat(List<Part> parts, int startIndex = 0) {
            var treeIndex = Lib.firstIndexOfTree(parts, startIndex);
            var slice = Lib.slice(parts, startIndex, treeIndex);
            if (treeIndex == -1) return new LastFlat(slice);

            var opIndex = Lib.firstIndexOfOp(slice);
            if (opIndex == -1) return new NoteFlat(slice, treeIndex);

            var subslice = Lib.slice(slice, 0, opIndex);
            if (Lib.isBlank(subslice)) return new OpFlat(slice, treeIndex, opIndex);

            return new NoteOpFlat(slice, treeIndex, opIndex);
        }

        static (TaoRawEntry, int) taoEntry(List<Part> parts, NextFlat flat) {
            var (key, valueIndex) = keyPart(parts, flat);
            var vi = valueIndex == -1? flat.treeIndex: valueIndex;
            var value = Lib.partsOfTree(parts[vi].asTree());

            return (new TaoRawEntry(key, value), vi + 1);
        }

        static (TaoKey, int) keyPart(List<Part> parts, Flat flat) {
            if (flat.isNote() || flat.isNoteOp()) return (stringKey(flat.slice), -1);

            if (flat.isOp()) {
                var slice = flat.slice;
                var opFlat = flat.asOp();
                var op = opFlat.op;
                if (Lib.isValidOp(op)) return (stringKey(slice), -1);

                if (op == '\'') {
                    if (Lib.isBlank(Lib.slice(slice, opFlat.opIndex + 1, slice.Count))) return paddedKey(parts, opFlat.treeIndex);
                    else throw new System.Exception("Only whitespace allowed before padded key, got: " + slice);
                }
                
                if (op == '#') return comment(parts, opFlat.treeIndex);
            }

            throw new System.Exception("Unrecognized key: " + flat);
        }

        static TaoKey stringKey(List<Part> parts) {
            // todo: meta

            var s = str(parts);

            var fvi = Lib.firstIndexOfVisible(s);
            var lvi = Lib.lastIndexOfVisible(s);

            var key = s.Substring(fvi, lvi + 1 - fvi);

            return new TaoKey(new TaoString(key));
        }

        static (TaoKey, int) paddedKey(List<Part> parts, int treeIndex) {
            throw new System.Exception("todo");
        }
        static (TaoKey, int) comment(List<Part> parts, int treeIndex) {
            throw new System.Exception("todo");
        }

        virtual public bool isEmpty() {return false;}
        virtual public bool isString() {return false;}
        virtual public bool isList() {return false;}
        virtual public bool isTable() {return false;}
        
        virtual public TaoTable asTable() {throw new System.Exception("not a Table");}
        virtual public TaoList asList() {throw new System.Exception("not a List");}
    }
    static class Lib {
        static public bool isValidOp(char c) {
            return c == '[' || c == ']' || c == '`';
        }
        static public int firstIndexOfTree(List<Part> parts, int startIndex = 0) {
            for (var i = startIndex; i < parts.Count; ++i) {
                if (parts[i].isTree()) return i;
            }
            return -1;
        }
        static public int firstIndexOfOp(List<Part> parts, int startIndex = 0) {
            for (var i = startIndex; i < parts.Count; ++i) {
                if (parts[i].isOp()) return i;
            }
            return -1;
        }
        static bool isVisible(char c) {
            return !(c == ' ' || c == '\n' || c == '\r' || c == '\t' || c == '\v');
        }
        static public int firstIndexOfVisible(string str) {
            for (var i = 0; i < str.Length; ++i) {
                if (isVisible(str[i])) return i;
            }
            return -1;
        }
        static public int lastIndexOfVisible(string str) {
            for (var i = str.Length - 1; i >= 0; --i) {
                if (isVisible(str[i])) return i;
            }
            return -1;
        }
        static public bool isBlank(List<Part> parts) {
            return parts.Count == 0 || (parts.Count == 1 && isBlankNote(parts[0]));
        }
        static public bool isBlankNote(Part part) {
            return part.isNote() && part.asNote().syms.Trim() == "";
        }
        // todo: perhaps cheap slices + always operate on them
        static public List<Part> slice(List<Part> parts, int si, int ei) {
            var s = new List<Part>();
            for (var i = si; i < ei; ++i) {
                s.Add(parts[i]);
            }
            return s;
        }
        static public List<Part> partsOfTree(Tree tree) {
            // todo: rename tree.tree back to tree.tao
            return tree.tao.parts;
        }
    }
    public class TaoEmpty: TaoData {
        override public bool isEmpty() {return true;}
    }
    public class TaoString: TaoData {
        public string str {get;}
        override public bool isString() {return true;}
        public TaoString(string str) {
            this.str = str;
        }
        override public string ToString() {
            string ret = "";
            foreach (var c in str)
            {
                if (Lib.isValidOp(c)) ret += '`' + c;
                else ret += c;
            }
            return ret;
        }
    }
    public class TaoList: TaoData {
        override public bool isList() {return true;}
        public override TaoList asList() {return this;}
        public List<TaoData> items {get;}
        public TaoList(List<TaoData> items) {
            this.items = items;
        }
        // todo: first, last
        // ?todo: has? rn to at
        public TaoData get(int index) {
            return items[index];
        }
        override public string ToString() {
            string ret = "";
            foreach (var item in items) {
                ret += "[" + item.ToString() + "]";
            }
            return ret;
        }
    }
    public class TaoKey {
        TaoString str;
        public TaoKey(TaoString str) {
            this.str = str;
        }
        override public string ToString() {
            var s = str.ToString();
            var key = s.Trim();
            if (key == "" || key != s) return "`'[" + s + "]";
            return key; 
        }
    }
    // todo: naming
    // parts or tao? probly parts -- also in js/ts tao parser; release v2
    class TaoRawEntry {
        public TaoKey key {get;}
        public List<Part> parts {get;}
        public TaoRawEntry(TaoKey key, List<Part> parts) {
            this.key = key;
            this.parts = parts;
        }
    }
    public class TaoEntry {
        public TaoKey key {get;}
        public TaoData value {get;}
        public TaoEntry(TaoKey key, TaoData value) {
            this.key = key;
            this.value = value;
        }
    }
    class Flat {
        public List<Part> slice {get;}
        protected Flat(List<Part> slice) {
            this.slice = slice;
        }
        virtual public bool isLast() {return false;}
        virtual public bool isNote() {return false;}
        virtual public bool isOp() {return false;}
        virtual public bool isNoteOp() {return false;}

        virtual public OpFlat asOp() {throw new System.Exception("not Op!");}
        virtual public NextFlat asNext() {throw new System.Exception("not NextFlat!");}
    }
    // todo: rn to AFlat or sth
    class NextFlat: Flat {
        public int treeIndex {get;}
        override public NextFlat asNext() {return this;}
        public NextFlat(List<Part> slice, int treeIndex): base(slice) {
            this.treeIndex = treeIndex;
        }
    }
    class AnOpFlat: NextFlat {
        public int opIndex {get;}
        public AnOpFlat(List<Part> slice, int treeIndex, int opIndex): base(slice, treeIndex) {
            this.opIndex = opIndex;
        }
    }
    class LastFlat: Flat {
        override public bool isLast() {return true;}
        public LastFlat(List<Part> slice): base(slice) {}
    }
    class NoteFlat: NextFlat {
        override public bool isNote() {return true;}
        public NoteFlat(
            List<Part> slice, 
            int treeIndex
        ): base(slice, treeIndex) {}
    }
    class OpFlat: AnOpFlat {
        public char op {get;}
        override public bool isOp() {return true;}
        override public OpFlat asOp() {return this;}
        public OpFlat(
            List<Part> slice, 
            int treeIndex, 
            int opIndex
        ): base(slice, treeIndex, opIndex) {
            this.op = slice[opIndex].asOp().sym;
        }
    }
    class NoteOpFlat: AnOpFlat {
        override public bool isNoteOp() {return true;}
        public NoteOpFlat(
            List<Part> slice, 
            int treeIndex, 
            int opIndex
        ): base(slice, treeIndex, opIndex) {}
    }
    public class TaoTable: TaoData {
        override public bool isTable() {return true;}
        override public TaoTable asTable() {return this;}
        public List<TaoEntry> entries {get;}
        public TaoTable(List<TaoEntry> entries) {
            this.entries = entries;
        }
        public bool has(string key) {
            foreach (var entry in entries) {
                if (entry.key.ToString() == key) return true;
            }
            return false;
        }
        // ?todo: rn firstAt
        public TaoData getFirst(string key) {
            foreach (var entry in entries) {
                if (entry.key.ToString() == key) return entry.value;
            }
            throw new System.Exception("No entry under key " + key);
        }
        // ?todo: alias get -- or lastAt = get
        // lastAt
        public TaoData getLast(string key) {
            for (int i = entries.Count - 1; i >= 0; --i) {
                var entry = entries[i];
                if (entry.key.ToString() == key) return entry.value;
            }
            throw new System.Exception("No entry under key " + key);
        }
        // allAt, at
        public TaoList getAll(string key) {
            var values = new List<TaoData>();
            foreach (var entry in entries) {
                if (entry.key.ToString() == key) values.Add(entry.value);
            }
            return new TaoList(values);
        }
        override public string ToString() {
            string ret = "";
            foreach (var entry in entries) {
                ret += entry.key.ToString() + "[" + entry.value.ToString() + "]";
            }
            return ret;
        }
    }
}
