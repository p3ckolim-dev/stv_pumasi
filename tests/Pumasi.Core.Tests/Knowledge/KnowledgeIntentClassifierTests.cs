using Pumasi.Core.Knowledge;
using Xunit;

namespace Pumasi.Core.Tests.Knowledge;

public sealed class KnowledgeIntentClassifierTests
{
    [Theory]
    [InlineData("온실 수확해줘")]
    [InlineData("기계 수거해줘")]
    [InlineData("water the dry crops")]
    public void Classify_ReturnsTaskPlanningForActionRequests(string input)
    {
        var classifier = new KnowledgeIntentClassifier();

        var intent = classifier.Classify(input);

        Assert.Equal(KnowledgeIntent.TaskPlanning, intent);
    }

    [Theory]
    [InlineData("딸기 씨앗은 어디서 사?")]
    [InlineData("아비게일이 좋아하는 선물은 뭐야?")]
    [InlineData("여름에 수익 좋은 작물 추천해줘")]
    public void Classify_ReturnsWikiAnswerForInformationQuestions(string input)
    {
        var classifier = new KnowledgeIntentClassifier();

        var intent = classifier.Classify(input);

        Assert.Equal(KnowledgeIntent.WikiAnswer, intent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("온실 어떻게 할까?")]
    [InlineData("너는 뭘 할 수 있어?")]
    [InlineData("품앗이 뭐야?")]
    public void Classify_ReturnsAmbiguousWhenIntentIsNotSafeToExecute(string input)
    {
        var classifier = new KnowledgeIntentClassifier();

        var intent = classifier.Classify(input);

        Assert.Equal(KnowledgeIntent.Ambiguous, intent);
    }
}
