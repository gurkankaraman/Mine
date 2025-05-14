# Mine: WhatsApp Chat Analysis with Google Gemini AI

A comprehensive WhatsApp chat analysis application powered by Google Gemini AI that offers relationship insights, smart reply suggestions, and personalized AI chat experiences.

## 📱 Overview

Mine is an AI-powered application that helps users analyze their WhatsApp conversations using Google's Gemini AI technology. The application can:

- Perform detailed conversation analysis based on relationship types
- Generate smart response suggestions for ongoing conversations
- Create personalized AI chat experiences that mimic real conversation partners
- Provide detailed metrics and statistics about conversations

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 8.0
- **Frontend**: Bootstrap 5, JavaScript, CSS3, HTML5
- **AI Integration**: Google Gemini AI 2.0 Flash
- **Testing**: xUnit, Moq
- **API Documentation**: Swagger/OpenAPI

## 🧩 Key Features

### 💬 Conversation Analysis
Upload or paste your WhatsApp chat history to receive a detailed analysis based on relationship types:
- Flirt analysis
- Conflict resolution insights
- Friendship dynamics
- Family relationships
- Professional communication
- General conversation patterns

### 💡 Smart Response Suggestions
Get AI-generated response options for your WhatsApp conversations:
- Multiple response options with different tones and approaches
- Personalized suggestions based on conversation history
- Contextually relevant replies

### 🤖 Artificial Chat
Experience a realistic simulation of chatting with someone:
- AI learns the communication style of the person from chat history
- Generates responses that mimic the person's tone, emoji usage, and communication patterns
- Ability to continue conversations automatically

### 📊 Metrics & Statistics
View detailed metrics about your conversations:
- Message counts by person
- Most used words and emojis
- Conversation activity patterns
- Time-based analysis

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Google Gemini API key

### Installation

1. Clone the repository:
```bash
git clone https://github.com/gurkankaraman/WhatsUpAPI.git
cd WhatsUpAPI
```

2. Update the API key in `appsettings.json`:
```json
"Gemini": {
  "ApiKey": "YOUR_API_KEY_HERE",
  "Url": "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent"
}
```

3. Build and run the application:
```bash
dotnet build
dotnet run
```

4. Navigate to `http://localhost:5001` in your browser

### How to Use

1. **Export WhatsApp Chat**: 
   - Open WhatsApp > Select a chat > Menu > More > Export chat
   - Choose "Without media" for faster uploads

2. **Select a Service**:
   - Conversation Analysis: Upload chat > select relationship type > analyze
   - Response Suggestions: Upload chat > get AI-suggested replies
   - Artificial Chat: Upload chat > select person > start chatting

3. **Review Results**:
   - Detailed analysis with metrics and insights
   - Multiple response options with different approaches
   - Interactive chat experience with AI-generated responses

## 📂 Project Structure

- `/Controllers`: MVC and API controllers
- `/Services`: Core service implementations (ConversationProcessor, GeminiClient)
- `/Views`: MVC view templates
- `/WhatsapSamples`: Sample conversations for testing
- `/Properties`: Application configuration

## 📡 API Endpoints

### ChatAnalysisController

- `POST /api/ChatAnalysis`: Analyze chat conversations
- `POST /api/ChatAnalysis/GenerateResponse`: Generate responses based on conversation context
- `POST /api/ChatAnalysis/ContinueConversation`: Automatically continue a conversation with AI

## 🧪 Testing

The project includes unit tests using xUnit:

```bash
dotnet test
```

## 👥 Contributors

- Gürkan Karaman - [GitHub](https://github.com/gurkankaraman)
- Baturhan Çağatay - [GitHub](https://github.com/BaturhanCagatay)

## 📞 Contact

- Email: gurkankaraman2002@gmail.com
- Email: baturhancagatay@gmail.com

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

Built with ❤️ and powered by Google Gemini AI