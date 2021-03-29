using System;
using TreeAnnotation;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            // todo: prettyprinting: implement X.ToString in terms of X.print/stringify which accepts indent/prefixes, etc.
            var list = TaoData.parse(" [test] [test2] [test3] ").asList();
            Console.WriteLine(list);
            Console.WriteLine(list.get(1));

            var plist = TaoData.parse("a [test] n [test2] x [test3] a [sec] ").asTable();
            Console.WriteLine(plist);
            Console.WriteLine(plist.getFirst("a"));
            Console.WriteLine(plist.getLast("a"));
            Console.WriteLine(plist.getAll("a"));
        }
    }
}
