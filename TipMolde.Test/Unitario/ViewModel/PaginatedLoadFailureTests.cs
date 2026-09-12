using FluentAssertions;
using NUnit.Framework;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
public class PaginatedLoadFailureTests
{
    [Test]
    public async Task SearchFailure_ShouldShowError_AndAllowRetry()
    {
        var vm = new FailingListViewModel();
        Func<Task> search = () => vm.PesquisarCommand.ExecuteAsync(null);

        await search.Should().NotThrowAsync();
        vm.ErrorMessage.Should().Be("Backend indisponivel");
        vm.IsLoading.Should().BeFalse();

        vm.Fail = false;
        await search();
        vm.ErrorMessage.Should().BeEmpty();
        vm.CompletedLoads.Should().Be(1);
    }

    [Test]
    public async Task NextPageFailure_ShouldNotEscapeCommand()
    {
        var vm = new FailingListViewModel { TotalPages = 3 };
        Func<Task> next = () => vm.NextPageCommand.ExecuteAsync(null);

        await next.Should().NotThrowAsync();
        vm.HasError.Should().BeTrue();
        vm.IsLoading.Should().BeFalse();
    }

    [Test]
    public async Task PendingLoad_ShouldPreventDuplicateRequests()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = new FailingListViewModel { Fail = false, Pending = completion.Task };
        var first = vm.LoadForTestAsync();
        await vm.LoadForTestAsync();
        vm.StartedLoads.Should().Be(1);
        completion.SetResult();
        await first;
        vm.IsLoading.Should().BeFalse();
        vm.CompletedLoads.Should().Be(1);
    }

    private sealed class FailingListViewModel : SearchableViewModel
    {
        public bool Fail { get; set; } = true;
        public Task Pending { get; set; } = Task.CompletedTask;
        public int StartedLoads { get; private set; }
        public int CompletedLoads { get; private set; }
        public Task LoadForTestAsync() => LoadPageAsync();

        protected override Task LoadPageAsync() => ExecutePagedLoadAsync(async () =>
        {
            StartedLoads++;
            await Pending;
            if (Fail)
                throw new HttpRequestException("Backend indisponivel");
            CompletedLoads++;
        });
    }
}
