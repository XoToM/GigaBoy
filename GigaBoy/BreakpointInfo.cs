namespace GigaBoy
{
    public class BreakpointInfo
    {
        public bool BreakOnRead { get; set; } = false;
        public bool BreakOnWrite { get; set; } = false;
        public bool BreakOnExecute { get; set; } = false;
        public bool BreakOnJump { get; set; } = false;
    }
}