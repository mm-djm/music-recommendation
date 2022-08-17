# music-recommendation

The Training Data is on 2022/08/18.

A simple web-based application which recommends music. Based on asp.net mvc.

CSV file is convert by CSVHelper.

The process:
- Find the artist that user like, and find who also like this artist in the training data.
- Then find the people who have the most and least similar interest.
- Compare these people and find out 5 artist that the user maybe interested in. Remember to delete the artist that mentioned in the survey before.
