# Educational Trivia Game Web Platform 

## 1. Project Description

This project aims to create an educational game platform where players can choose different categories and compete against each other by answering trivia questions or solving problems within a time limit. It is not enough to choose a correct answer, but to arrive at the solution faster than your opponents. Players will compete for the top places on the leaderboard or participate in real-time duels with each other. Our goal is to create a platform that integrates education, game elements, and competition under a single app.

## 2. User Flow

When entering the platform, the user can either sign in or sign up with a new account, after which they are redirected to the main page. There, the player can choose one of available categories - math, history, lithuanian language and geography. Every category has a distinct game mode with unique rules and questions. For now, the only available category is geography but more will come in the future. In order to start the game, the user may create their own room and share their code with other players or join an existing room. During the game's runtime, the user's goal is to collect as many points as possible by answering the questions quickly and accurately. At the end of the game the player sees their final score which will be saved and added to the leaderboard.

## 3. Web Pages

1. **Sign in / Sign up**
2. **Main Page**
    - Choose a category;
    - Create a room;
    - Start a new game;
    - See user statistics;
    - View leaderboards.
3. **Category Selection**
4. **Game Room**
    - Game code;
    - Connected players;
    - Ready button
5. **Game Window**
    - Question/problem
    - Possible answers
    - Timer
    - Current results
    - Round number
    - Other players' scores
6. **Final results**

## 4. Project Architecture

### 4.1. Backend

1. User management
2. Game creation
3. Joining a game
4. Selection of questions
5. Checking answers
6. Score calculation
7. Saving results
8. Leaderboard generation

### 4.2. Frontend

1. Page rendering
2. User interface
3. Game state rendering

### 4.3. Database

1. **Users**
    - Id
    - Username
    - PasswordHash
    - CreatedAt

2. **Questions**
    - Id
    - QuestionText
    - CorrectAnswer
    - Category
    - Difficulty

3. **Games**
    - Id
    - GameMode
    - Category
    - CreatedAt
    - Status

4. **GamePlayers**
    - GameId
    - UserId
    - Score

5. **GameResults**
    - Id
    - GameId
    - UserId
    - Score
    - CorrectAnswers
    - AverageResponseTime

## 5. Technology Stack
List the technologies and tools used:
- **Backend**: C# ASP.NET Core (.NET 10.0)
- **Frontend**: React.ts
- **Database**: PostgreSQL, Supabase

