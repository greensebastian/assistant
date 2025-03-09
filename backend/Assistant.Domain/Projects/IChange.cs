using FluentResults;

namespace Assistant.Domain.Projects;

public interface IChange<in TProject>
{
    public Result Apply(TProject project);

    public string Description(TProject project);
}