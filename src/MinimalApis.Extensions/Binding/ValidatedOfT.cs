﻿using System.Reflection;
using MinimalApis.Extensions.Metadata;
using MiniValidation;

namespace MinimalApis.Extensions.Binding;

/// <summary>
/// Represents a validated object of the type specified by <typeparamref name="TValue"/> as a parameter to an ASP.NET Core route handler delegate.
/// </summary>
/// <typeparam name="TValue">The type of the object being validated.</typeparam>
public struct Validated<TValue> : IProvideEndpointParameterMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Validated{TValue}"/> class.
    /// </summary>
    /// <param name="value">The object to validate.</param>
    /// <param name="initialErrors">Any initial object-level errors to populate the <see cref="Errors"/> collection with.</param>
    /// <param name="defaultBindingResultStatusCode">The <see cref="HttpResponse.StatusCode"/> set by the default binder, if any.</param>
    public Validated(TValue? value, string[]? initialErrors = null, int? defaultBindingResultStatusCode = null)
    {
        var isValid = true;
        Value = value;

        if (Value != null)
        {
            isValid = MiniValidator.TryValidate(Value, out var errors);
            Errors = errors;
        }
        else
        {
            Errors = new Dictionary<string, string[]>();
        }

        if (initialErrors != null)
        {
            isValid = false;
            Errors.Add("", initialErrors);
        }

        DefaultBindingResultStatusCode = defaultBindingResultStatusCode;
        IsValid = isValid;
    }

    /// <summary>
    /// The validated object.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Indicates whether the object is valid or not. <c>true</c> if the object is valid; <c>false</c> if it is not.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// A dictionary that contains details of each failed validation.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Gets the response status code set by the default binding logic if there were any binding issues. This value will
    /// be <c>null</c> if the default binding logic did not detect an issue.
    /// </summary>
    public int? DefaultBindingResultStatusCode { get; }

    /// <summary>
    /// Deconstructs the <see cref="Value"/> and <see cref="IsValid"/> properties.
    /// </summary>
    /// <param name="value">The value of <see cref="Value"/>.</param>
    /// <param name="isValid">The value of <see cref="IsValid"/>.</param>
    public void Deconstruct(out TValue? value, out bool isValid)
    {
        value = Value;
        isValid = IsValid;
    }

    /// <summary>
    /// Deconstructs the <see cref="Value"/>, <see cref="IsValid"/>, and <see cref="Errors"/> properties.
    /// </summary>
    /// <param name="value">The value of <see cref="Value"/>.</param>
    /// <param name="isValid">The value of <see cref="IsValid"/>.</param>
    /// <param name="errors">The value of <see cref="Errors"/>.</param>
    public void Deconstruct(out TValue? value, out bool isValid, out IDictionary<string, string[]> errors)
    {
        value = Value;
        isValid = IsValid;
        errors = Errors;
    }

    /// <summary>
    /// Binds the specified parameter from <see cref="HttpContext.Request"/>. This method is called by the framework on your behalf
    /// when populating parameters of a mapped route handler.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> to bind the parameter from.</param>
    /// <param name="parameter">The route handler parameter being bound to.</param>
    /// <returns>An instance of <see cref="Validated{TValue}"/> if one is deserialized from the request, otherwise <c>null</c>.</returns>
    /// <exception cref="BadHttpRequestException">Thrown when the request Content-Type header is not a recognized JSON media type.</exception>
    public static async ValueTask<Validated<TValue?>> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));

        var (value, statusCode) = await DefaultBinder<TValue>.GetValueAsync(context, parameter);

        if (statusCode != StatusCodes.Status200OK)
        {
            // Binding issue, add an error
            return new Validated<TValue?>(default, new[] { $"An error occurred while processing the request." }, statusCode);
        }

        return new Validated<TValue?>(value, null, null);
    }

    /// <summary>
    /// Provides metadata for parameters to <see cref="Endpoint"/> route handler delegates.
    /// </summary>
    /// <param name="parameter">The parameter to provide metadata for.</param>
    /// <param name="services">The <see cref="IServiceProvider"/>.</param>
    /// <returns>The metadata.</returns>
    public static IEnumerable<object> GetMetadata(ParameterInfo parameter, IServiceProvider services) =>
        IProvideEndpointParameterMetadata.GetDefaultMetadataForWrapperType<TValue>(parameter, services);
}
