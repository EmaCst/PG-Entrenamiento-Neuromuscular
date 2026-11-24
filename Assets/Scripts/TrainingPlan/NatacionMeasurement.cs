using System;

// Explicit tags only: missing tags belong to an unknown/older measurement method.
public static class NatacionMeasurement
{
    public const string Hands = "pose_hand_screen_contact_v1";
    public const string Feet = "pose_foot_screen_contact_v1";
    public const string Turns = "pose_body_turn_response";
    public static bool Matches(string actual, string expected)
        => !string.IsNullOrEmpty(actual) && string.Equals(actual, expected, StringComparison.Ordinal);
}
