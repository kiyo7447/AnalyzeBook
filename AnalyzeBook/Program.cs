using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnalyzeBook
{
	class Program
	{

		/// <summary>
		/// アルゴリズムをまとめる
		/// 
		/// 既存フォルダを解析して、オブジェクト化
		///     フォルダ名、日本語名、英名、巻数（配列化）
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

			var program = new Program();
			bool isUpdate = true;
			program.Run(args, isUpdate);


		}
		void Run(string[] args, bool isUpdate)
		{
			//エラーがあったら止める。
			bool errFlg = false;
			/// 本の種類
			List<Book> _books = new List<Book>();
			/// 未振り分けの本
			List<BookNode> _mifuriwakeFiles = new List<BookNode>();
			var _mangaFolder = new DirectoryInfo(@"D:\まんが");
			var _mifuriwakeFolder = new DirectoryInfo(@"D:\まんが_未振り分け");


			Console.WriteLine("■Phase.1 まんがフォルダを解析して。_books(本の種類)を作ります。");
			List<DirectoryInfo> mangaFolder = new List<DirectoryInfo>(_mangaFolder.GetDirectories());
			mangaFolder.All(dic =>
			{
				Console.WriteLine($"まんがフォルダ=「{dic.Name}」から本の種類を取り出します。");
				dic.GetDirectories().All(bookDir =>
				{
					bookDir.GetFiles().All(file =>
					{
						var bookNode = AnalyzeBook(file);
						//_booksFiles.Add(bookNode);

						if (_books.Where(b => b.AlphabetName == bookNode.AlphabetName).Count() == 0)
						{
							_books.Add(new Book() { DirectoryInfo = bookDir, AlphabetName = bookNode.AlphabetName, JapaneseName = bookDir.Name });
						}
						else if (_books.Where(b => b.AlphabetName == bookNode.AlphabetName && b.DirectoryInfo.FullName == bookNode.FileInfo.DirectoryName).Count() > 0)
						{
							//タイトル名とディレクトリが一緒の場合は、無視する。
						}
						else
						{
							errFlg = true;
							WriteLineError($"file={file.FullName},books={_books.Where(b => b.AlphabetName == bookNode.AlphabetName).FirstOrDefault().DirectoryInfo.FullName}");
						}
						return true;
					});
					return true;
				});
				return true;
			});
			Console.WriteLine($"解析したまんがフォルダ数={mangaFolder.Count}");
			Console.WriteLine($"本の種類={_books.Count}");
			Console.WriteLine($"本の種類の解析を完了しました。");


			if (errFlg == true)
			{
				WriteLineError("エラーが有ったので処理を中断します。");
				Console.ReadKey();
				return;
			}


			Console.WriteLine("■Phase.2 未振り分けの書籍を_mifuriwakeFielsに保存します。");
			List<DirectoryInfo> mifuriwakeFolder = new List<DirectoryInfo>(_mifuriwakeFolder.GetDirectories());
			mifuriwakeFolder.All(dic =>
			{
				Console.WriteLine($"未振り分けまんがフォルダ={dic.Name}, 冊数={dic.GetFiles().Length}");
				dic.GetFiles().All(file =>
				{
					var bookNode = AnalyzeBook(file);
					_mifuriwakeFiles.Add(bookNode);
					return true;
				});
				return true;
			});
			Console.WriteLine($"未振り分けまんがフォルダ数={mifuriwakeFolder.Count}, 冊数={_mifuriwakeFiles.Count}");

			Console.WriteLine($"未振り分け本：ロードが完了した冊数={_mifuriwakeFiles.Where(e => e.IsLoaded == true).Count()}");
			_mifuriwakeFiles.Where(e => e.IsLoaded == true).All(bn =>
			{
				//Console.WriteLine($"LoadedFileName={bn.FileInfo.Name}\tTitle={bn.AlphabetName}");
				return true;
			});
			Console.WriteLine($"未振り分け本：ロードが失敗した冊数={_mifuriwakeFiles.Where(e => e.IsLoaded == false).Count()}");
			_mifuriwakeFiles.Where(e => e.IsLoaded == false).All(bn =>
			{
				//Console.WriteLine($"LoadedFileName={bn.FileInfo.Name}\tTitle={bn.AlphabetName}");
				return true;
			});


			Console.WriteLine("■Phase3 本の種類(_books)にある該当本(_mifuriwakeFiles)を移動します。");
			int cnt = 0;
			_mifuriwakeFiles.All(f =>
			{
				var books = _books.Where(e => e.AlphabetName == f.AlphabetName);
				if (books.Count() == 1)
				{
					//移動可能
					Console.WriteLine($"fromBook={f.FileInfo.Name}. toFolder={books.FirstOrDefault().DirectoryInfo.FullName}");
					if (isUpdate == true)
						f.FileInfo.MoveTo(Path.Combine(books.FirstOrDefault().DirectoryInfo.FullName, f.FileInfo.Name), true);
					else
						WriteLineUpdate($"本を移動します。{f.FileInfo.FullName} to {Path.Combine(books.FirstOrDefault().DirectoryInfo.FullName, f.FileInfo.Name)}");
					cnt++;
				}
				else if (books.Count() > 1)
					//エラーが発生するわけがない
					WriteLineError($"fromBook={f.FileInfo.Name}. toFolder={books.FirstOrDefault().DirectoryInfo.FullName}");
				else
				{
					//移動先がない。
					//Console.WriteLine($"書籍の名前={f.FileInfo.Name}");
				}

				return true;
			});
			Console.WriteLine($"移動対象の未振り分け本={_mifuriwakeFiles.Count()}冊, 移動数={cnt}");

			Console.WriteLine("■Phase4 3巻以上の本を「まんが（新作）」フォルダへフォルダを作成してその中に振り分けます。");
			cnt = 0;
			_mifuriwakeFiles.Where(e => { return e.MaxNum > 2; }).All(f =>
			{
				//移動可能
				if (new DirectoryInfo(Path.Combine(new DirectoryInfo(@"D:\まんが\まんが（新作）").FullName, f.AlphabetName)).Exists == false)
					new DirectoryInfo(Path.Combine(new DirectoryInfo(@"D:\まんが\まんが（新作）").FullName, f.AlphabetName)).Create();
				Console.WriteLine($"fromBook={f.FileInfo.Name}. toFolder={Path.Combine(new DirectoryInfo(@"D:\まんが\まんが（新作）").FullName, f.AlphabetName, f.FileInfo.Name)}");
				if (isUpdate == true)
					f.FileInfo.MoveTo(Path.Combine(new DirectoryInfo(@"D:\まんが\まんが（新作）").FullName, f.AlphabetName, f.FileInfo.Name), true);
				else
					WriteLineUpdate($"本を移動します。{f.FileInfo.FullName} to {Path.Combine(new DirectoryInfo(@"D:\まんが\まんが（新作）").FullName, f.AlphabetName, f.FileInfo.Name)}");

				cnt++;

				return true;
			});
			Console.Write($"移動対象の未振り分け本={_mifuriwakeFiles.Count()}冊, 移動数={cnt}");

		}

		/// <summary>
		/// エラーメッセージを赤字で表示する
		/// </summary>
		/// <param name="message"></param>
		void WriteLineError(string message)
		{
			_WriteLine(message, ConsoleColor.Red);
		}
		void WriteLineUpdate(string message)
		{
			_WriteLine(message, ConsoleColor.Blue);
		}
		void _WriteLine(string message, ConsoleColor color)
		{
			var backColer = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(message);
			Console.ForegroundColor = backColer;

		}
		BookNode AnalyzeBook(FileInfo file)
		{
			Dictionary<Regex, Func<Match, BookNode>> proc = new Dictionary<Regex, Func<Match, BookNode>>();

			//一冊もの
			proc.Add(new Regex(@"^(?<title>.+)(_| )(ch|v|)(?<num>[0-9][0-9])( fix|se|ss|s|b|e|\+a| lq|_lq|)\.(zip|rar)$", RegexOptions.IgnoreCase),
				(Match match) =>
				{
					BookNode bn = new BookNode()
					{
						FileInfo = file,
						AlphabetName = match.Groups["title"].Value,
						MinNum = int.Parse(match.Groups["num"].Value),
						MaxNum = int.Parse(match.Groups["num"].Value),
						IsLoaded = true
					};
					return bn;
				});

			//複数巻
			proc.Add(new Regex(@"^(?<title>.+)( |_)(ch|v|)(?<minnum>[0-9][0-9])-(?<maxnum>[0-9][0-9])( fix|se|ss|s|b|e|\+a| lq|_lq|)\.(zip|rar)$", RegexOptions.IgnoreCase),
				(Match match) =>
				{
					BookNode bn = new BookNode()
					{
						FileInfo = file,
						AlphabetName = match.Groups["title"].Value,
						MinNum = int.Parse(match.Groups["minnum"].Value),
						MaxNum = int.Parse(match.Groups["maxnum"].Value),
						IsLoaded = true
					};
					return bn;
				});
			//YYYY XXXの雑誌 Hitotsubashi Business Review 2020 AUT.zip用
			proc.Add(new Regex(@"^(?<title>.+) (?<yyyymm>[0-9][0-9][0-9][0-9] [a-z][a-z][a-z])\.(zip|rar)$", RegexOptions.IgnoreCase),
				(Match match) =>
				{
					BookNode bn = new BookNode()
					{
						FileInfo = file,
						AlphabetName = match.Groups["title"].Value,
						Date = match.Groups["yyyymm"].Value,
						IsLoaded = true
					};
					return bn;
				});
			//YYYY-MMの雑誌
			proc.Add(new Regex(@"^(?<title>.+) (?<yyyymm>[0-9][0-9][0-9][0-9]-[0-9][0-9])\.(zip|rar)$", RegexOptions.IgnoreCase),
				(Match match) =>
				{
					BookNode bn = new BookNode()
					{
						FileInfo = file,
						AlphabetName = match.Groups["title"].Value,
						Date = match.Groups["yyyymm"].Value,
						IsLoaded = true
					};
					return bn;
				});
			//YYYY-MM-DDの雑誌
			proc.Add(new Regex(@"^(?<title>.+) (?<yyyymmdd>[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9])\.(zip|rar)$", RegexOptions.IgnoreCase),
				(Match match) =>
				{
					BookNode bn = new BookNode()
					{
						FileInfo = file,
						AlphabetName = match.Groups["title"].Value,
						Date = match.Groups["yyyymmdd"].Value,
						IsLoaded = true
					};
					return bn;
				});
			//残りはその他

			//解析をまとめて実行
			foreach (var (regex, func) in proc)
				if (regex.IsMatch(file.Name))
					return func(regex.Match(file.Name));

			//残りはその他書籍
			BookNode bn = new BookNode()
			{
				FileInfo = file,
				AlphabetName = file.Name,
				IsLoaded = false
			};
			return bn;
		}
	}



	class Book
	{
		public string AlphabetName { get; set; }
		public DirectoryInfo DirectoryInfo { get; set; }
		public string JapaneseName { get; set; }
		//		public List<BookNode> Books { get; set; }
	}
	class BookNode
	{
		public FileInfo FileInfo { get; set; }
		public string AlphabetName { get; set; }
		/// <summary>
		/// 一冊の場合はMinNum=MaxNum
		/// </summary>
		public int MinNum { get; set; }
		public int MaxNum { get; set; }
		/// <summary>
		/// YYYY-MMやYYYY-MM-DDが格納される
		/// </summary>
		public string Date { get; set; }
		/// <summary>
		/// 正規表現による読み込みに成功した=true
		/// </summary>
		public bool IsLoaded { get; set; } = false;
	}

}
