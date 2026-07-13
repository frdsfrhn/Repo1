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

  /// Turns whatever an agent typed into the phone field (with spaces,
  /// dashes, a leading `+`, or a leading `0` for a local number with no
  /// country code) into the bare digit string wa.me links expect
  /// (`https://wa.me/60123456789`, no `+`, no leading zero).
  ///
  /// Assumes Malaysia (`60`) when no country code is present, since that's
  /// this app's target market — not a general-purpose phone parser.
  static String whatsAppDigits(String rawPhone) {
    var digits = rawPhone.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.startsWith('0')) {
      digits = '60${digits.substring(1)}';
    }
    return digits;
  }
}
