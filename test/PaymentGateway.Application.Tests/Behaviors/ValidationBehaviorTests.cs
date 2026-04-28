using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using PaymentGateway.Application.Behaviors;

namespace PaymentGateway.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private static readonly RequestHandlerDelegate<string> NextDelegate = _ => Task.FromResult("ok");

    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);
        var result = await behavior.Handle(new TestRequest(), NextDelegate, CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenValidatorPasses_CallsNext()
    {
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>([validatorMock.Object]);
        var result = await behavior.Handle(new TestRequest(), NextDelegate, CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenValidatorFails_ThrowsValidationException()
    {
        var failures = new List<ValidationFailure>
        {
            new("CardNumber", "Card number is required")
        };

        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, string>([validatorMock.Object]);

        Func<Task> act = () => behavior.Handle(new TestRequest(), NextDelegate, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Card number is required*");
    }

    [Fact]
    public async Task Handle_WhenMultipleValidatorsAndOneFails_ThrowsValidationException()
    {
        var passing = new Mock<IValidator<TestRequest>>();
        passing
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var failing = new Mock<IValidator<TestRequest>>();
        failing
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Amount", "Amount must be positive")]));

        var behavior = new ValidationBehavior<TestRequest, string>([passing.Object, failing.Object]);

        Func<Task> act = () => behavior.Handle(new TestRequest(), NextDelegate, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    public class TestRequest { }
}
