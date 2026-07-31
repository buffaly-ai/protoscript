namespace ProtoScript
{
	public class IncludeStatement : Statement
	{
		public string FileName;
		public bool Recursive = false;
		public bool Lazy = false;
	}
}
