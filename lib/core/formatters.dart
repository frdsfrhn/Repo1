import 'package:intl/intl.dart';

class Formatters {
  Formatters._();

  static final NumberFormat _currency =
      NumberFormat.currency(locale: 'ms_MY', symbol: 'RM ', decimalDigits: 0);

  static final DateFormat _date = DateFormat('d MMM yyyy');
  static final DateFormat _dateTime = DateFormat('d MMM yyyy, h:mm a');

  static String currency(num value) => _currency.format(value);

  static String date(DateTime? value) =>
      value == null ? '—' : _date.format(value);

  static String dateTime(DateTime value) => _dateTime.format(value);

  static String percent(num value) => '${value.toStringAsFixed(1)}%';

  static String relativeDueDate(DateTime? date) {
    if (date == null) return 'No follow-up set';
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final due = DateTime(date.year, date.month, date.day);
    final diff = due.difference(today).inDays;
    if (diff == 0) return 'Due today';
    if (diff == 1) return 'Due tomorrow';
    if (diff < 0) return '${-diff}d overdue';
    return 'Due in ${diff}d';
  }
}
