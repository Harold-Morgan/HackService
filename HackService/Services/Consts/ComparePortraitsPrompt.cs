namespace HackService.Services;

public class ComparePortraitsPrompt
{
    internal static string PropmtHeader =
        "На основе предоставленных строк проанализируй в цифровом формате сходимость интересов двух пользователей чата " +
        "Входные данные: ";

    internal static string PromptEnd =
        "Ответ верни в виде единственного числа decimal формата с точностью до двух знаков после запятой";
}