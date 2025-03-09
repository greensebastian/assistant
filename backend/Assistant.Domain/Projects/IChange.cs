using FluentResults;

namespace Assistant.Domain.Projects;

public interface IChange<in TProject>
{
    public Result ApplyTo(TProject project);

    public string Description(TProject project);
}