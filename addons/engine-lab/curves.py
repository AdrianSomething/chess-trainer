"""Serializable piecewise-linear schedules shared by task configuration and code templates."""
import math

def value(schedule, x, default=0):
    if schedule is None:
        return default
    if isinstance(schedule, (int, float)):
        return float(schedule)
    points = sorted(schedule.get("points") or [], key=lambda p: p[0])
    if not points:
        return float(schedule.get("value", default))
    end = points[-1][0]
    if schedule.get("cycle", False) and end > points[0][0]:
        x = 1 + (x - 1) % end
    if x <= points[0][0]:
        return float(points[0][1])
    for left, right in zip(points, points[1:]):
        if x <= right[0]:
            t = (x-left[0]) / (right[0]-left[0])
            return float(left[1] + t*(right[1]-left[1]))
    return float(points[-1][1])

def validate(schedule):
    if isinstance(schedule, (float, int)):
        if not math.isfinite(schedule): raise ValueError("Schedule must be finite.")
        return
    if not isinstance(schedule, dict): raise ValueError("Schedule must be a number or an object.")
    previous = -float("inf")
    for point in schedule.get("points", []):
        if len(point)!=2 or not all(math.isfinite(v) for v in point) or point[0]<=previous:
            raise ValueError("Curve points need finite values and increasing x coordinates.")
        previous=point[0]
    if not math.isfinite(schedule.get("value",0)): raise ValueError("Invalid constant value.")

def blend_distribution(start, end, fraction, x):
    return max(0,(1-fraction)*value(start,x,1)+fraction*value(end,x,1))
