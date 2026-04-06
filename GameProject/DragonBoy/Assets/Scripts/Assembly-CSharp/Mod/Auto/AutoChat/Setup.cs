using System.IO;
using System.Text.RegularExpressions;
using Mod.R;

namespace Mod.Auto.AutoChat
{
    public class Setup : IChatable
    {
        public static string[] inputTextAutoChat = new string[] { Strings.inputContent, "" };

        public static string[] inputDelayAutoChat = new string[] { Strings.inputDelay, Strings.timeMilliseconds + " (> 5000)" };

        public static int delayAutoChat = 5000;//default 5000ms = 5s
        public static Setup gI { get; } = new Setup();

		public static void loadFile()
		{
			string[] lines = ModDataStorage.ReadLinesOrDefault(Utils.PathAutoChat, new[]
			{
				Strings.communityMod,
				"6500"
			});
			delayAutoChat = int.Parse(lines[1]);
		}

        /// <summary>
        /// Kích hoạt khi người chơi tắt chức năng hoặc tắt game sẽ xóa các dòng auto chat 
        /// </summary>
		public static void clearStringTrash()
		{
			string content = ModDataStorage.ReadTextOrDefault(Utils.PathChatHistory);
			//Regex.Replace() thay thế các chuỗi tìm được bằng chuỗi rỗng, loại bỏ chúng khỏi chuỗi đầu vào
			string output = Regex.Replace(content, @",?\s*mcd\d{2}\:\s*[^""]*""[^""]*""", "");
			// Ghi nội dung vào file output
			ModDataStorage.WriteText(Utils.PathChatHistory, output);
		}

        public void onCancelChat()
        {
            ChatTextField.gI().isShow = false;
            ChatTextField.gI().ResetTF();
        }

        public void onChatFromMe(string text, string to)
        {
			string[] lines = ModDataStorage.ReadLinesOrDefault(Utils.PathAutoChat, new[]
			{
				Strings.communityMod,
				"6500"
			});
            if (string.IsNullOrEmpty(ChatTextField.gI().tfChat.getText()) || string.IsNullOrEmpty(text))
                return;
            if (ChatTextField.gI().strChat.Contains(inputTextAutoChat[0]))
            {
                try
                {
                    string newLine = ChatTextField.gI().tfChat.getText();
                    lines[0] = newLine; // chỉnh sửa dòng đầu tiên
					ModDataStorage.WriteLines(Utils.PathAutoChat, lines);
                    GameCanvas.startOKDlg(Strings.contentSaved + ": " + newLine);
                }
                catch
                {
                    GameCanvas.startOKDlg(Strings.errorOccurred + '!');
                }
            }
            else if (ChatTextField.gI().strChat.Contains(inputDelayAutoChat[0]))
            {
                try
                {
                    string newContent = ChatTextField.gI().tfChat.getText(); 
                    lines[1] = newContent; // chỉnh sửa dòng thứ 2
					ModDataStorage.WriteLines(Utils.PathAutoChat, lines);
                    delayAutoChat = int.Parse(newContent);
                    if (delayAutoChat < 5000)
                        delayAutoChat = 5000;
                    //Dù Interval là Int thì làm tròn hết nhưng mà cứ in ra cho nó chuyên nghiệp
                    GameScr.info1.addInfo(string.Format(Strings.valueChanged, "delay", (float)delayAutoChat / 1000), 0);
                }
                catch
                {
                    GameCanvas.startOKDlg(Strings.errorOccurred + '!');
                }
            }
            ChatTextField.gI().ResetTF();
        }
    }
}
