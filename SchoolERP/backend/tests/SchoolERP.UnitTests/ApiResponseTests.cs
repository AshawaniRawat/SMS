using SchoolERP.Shared.Models;
using Xunit;
namespace SchoolERP.UnitTests;
public class ApiResponseTests
{
    [Fact] public void OkCreatesSuccessfulResponse()
    {
        var response = ApiResponse<string>.Ok("ready", "Success");
        Assert.True(response.Success); Assert.Equal("ready", response.Data); Assert.Equal("Success", response.Message); Assert.Null(response.Errors);
    }
}