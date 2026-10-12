namespace SchoolERP.UnitTests.TestCommon;
public sealed class TestClock(DateTimeOffset? value=null):TimeProvider
{
    private DateTimeOffset _now=value??new DateTimeOffset(2026,1,15,10,0,0,TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow()=>_now;
    public void Advance(TimeSpan value)=>_now=_now.Add(value);
}
